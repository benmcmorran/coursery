import requests
from bs4 import BeautifulSoup
import os
import re

class Evaluation:
    def toJson(self):
        template = '{{yr:{},su:"{}",cr:"{}",sc:"{}",in:"{}",rc:{:.1f},ri:{:.1f},wl:{:.0f},gr:"{}"}}'
        return template.format(
            self.year, self.subject, self.course, self.section, self.instructor, self.courseQuality,
            self.instructorQuality, self.workload, self.grade)
    def toFilename(self):
        return "{}-{}-{}-{}.html".format(
            self.year, self.subject, self.course, self.section)

class InvalidLoginError(Exception):
    pass

session = requests.Session()

pages = {
    'home': 'https://bannerweb.wpi.edu',
    'login': 'https://bannerweb.wpi.edu/pls/prod/twbkwbis.P_WWWLogin',
    'validate': 'https://bannerweb.wpi.edu/pls/prod/twbkwbis.P_ValLogin',
    'year': 'https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_Year',
    'course': 'https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_CrseInst',
    'section': 'https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_CrseSect'
}

def runGetUrls():
    username = raw_input('username: ')
    password = raw_input('password: ')
    
    try:
        login(username, password)
        print('Login succeeded')
        with open('urls.txt', 'w') as output:
            print('Getting academic year listing')
            for year in years():
                print('Getting course listing for ' + year)
                for course in courses(year):
                    print('Getting sections for ' + course + ' in ' + year)
                    for section in sections(course, year):
                        output.write(section + '\n')
    except InvalidLoginError:
        print('Login failed')

def runDownloadUrls():
    username = raw_input('username: ')
    password = raw_input('password: ')
    filename = raw_input('url file: ')
    
    try:
        login(username, password)
        print('Login succeeded')
        downloadEvaluations(filename)
    except InvalidLoginError:
        print('Login failed')

def runProcessDownloads():
    try:
        os.mkdir('evaluations')
    except Exception:
        pass
    
    filenames = os.listdir(os.path.join(os.getcwd(), 'temp'))
    json = "courses=[\n"
    for name in filenames:
        try:
            evaluation = None
            path = os.path.join('temp', name)
            with open(path, 'r') as html:
                evaluation = parseEvaluation(html.read())
                json += evaluation.toJson() + ',\n'
            os.rename(path, os.path.join('evaluations', evaluation.toFilename()))
        except Exception:
            print('Error processing ' + name)
    json += '];'
    
    with open('courses.json', 'w') as courses:
        courses.write(json)

def parseEvaluation(text):
    result = Evaluation()
    
    result.year = int(re.search(r'Academic Year \d{4}-(\d{4})', text).group(1))

    match = re.search(r'([A-Z]+)-(\w+)', text)
    result.subject = match.group(1)
    result.course = match.group(2)

    result.section = re.search(r'Section (\w+)', text).group(1)
    result.instructor = re.search(r'Prof\. ([^<]+)', text).group(1)

    matches = re.finditer(r'<p.*?> ([\d\.]+)</p>', text)
    match = matches.next()
    result.courseQuality = float(match.group(1))
    match = matches.next()
    result.instructorQuality = float(match.group(1))

    aCount = int(re.search(r'A</p>.*?(\d+)', text, re.DOTALL).group(1))
    bCount = int(re.search(r'B</p>.*?(\d+)', text, re.DOTALL).group(1))
    cCount = int(re.search(r'C</p>.*?(\d+)', text, re.DOTALL).group(1))
    nrCount = int(re.search(r'NR/D/F</p>.*?(\d+)', text, re.DOTALL).group(1))
    otherCount = int(re.search(r"Other/Don't know</p>.*?(\d+)", text, re.DOTALL)
                       .group(1))

    inClassTime = re.search(r'26A\..*?3 hr/wk or less</p>.*?(\d+).*?' +
                            r'4 hr/wk</p>.*?(\d+).*?5 hr/wk</p>.*?(\d+).*?' +
                            r'6 hr/wk</p>.*?(\d+).*?' +
                            r'7 hr/wk or more</p>.*?(\d+)', text, re.DOTALL)
    outClassTime = re.search(r'26B\..*?0 hr/wk</p>.*?(\d+).*?' +
                             r'1-5 hr/wk</p>.*?(\d+).*?' +
                             r'6-10 hr/wk</p>.*?(\d+).*?' +
                             r'11-15 hr/wk</p>.*?(\d+).*?' +
                             r'16-20 hr/wk</p>.*?(\d+).*?' +
                             r'21 hr/wk or more</p>.*?(\d+)', text, re.DOTALL)
    oldClassTime = re.search(r'26\..*?8 hrs\. or fewer</p>.*?(\d+).*?' +
                             r'9-12 hrs\.</p>.*?(\d+).*?' +
                             r'13-16 hrs\.</p>.*?(\d+).*?' +
                             r'17-20 hrs\.</p>.*?(\d+).*?' +
                             r'21 hrs\. or more</p>.*?(\d+)', text, re.DOTALL)

    if inClassTime and outClassTime:
        inCount = sum([int(inClassTime.group(i+1)) for i in range(5)])
        inTime = sum([int(inClassTime.group(i+1))*[2,4,5,6,7][i] for i in range(5)])
        outCount = sum([int(outClassTime.group(i+1)) for i in range(6)])
        outTime = sum([int(outClassTime.group(i+1))*[0,3,8,13,18,22][i] for i in range(6)])
        result.workload = float(inTime) / inCount + float(outTime) / outCount
    else:
        count = sum([int(oldClassTime.group(i+1)) for i in range(5)])
        time = sum([int(oldClassTime.group(i+1))*[6,10,15,18,22][i] for i in range(5)])
        result.workload = float(time) / count
    
    if nrCount >= aCount and nrCount >= bCount and nrCount >= cCount:
        result.grade = "NR"
    if cCount >= aCount and cCount >= bCount and cCount >= nrCount:
        result.grade = "C"
    if bCount >= aCount and bCount >= cCount and bCount >= nrCount:
        result.grade = "B"
    if aCount >= bCount and aCount >= cCount and aCount >= nrCount:
        result.grade = "A"

    return result

def downloadEvaluations(urlFile):
    with open(urlFile, 'r') as urls:
        i = 1
        directory = 'temp'
        try:
            os.mkdir(directory)
        except Exception:
            pass
        for url in urls.read().splitlines():
            print('Downloading evaluation ' + str(i))
            downloadEvaluation(url, os.path.join(directory, str(i) + '.htm'))
            i += 1

def downloadEvaluation(url, name):
    response = session.get(pages['home'] + url, stream=True)
    with open(name, 'w') as output:
        for block in response.iter_content(1024):
            if block:
                output.write(block)
                output.flush()

def login(username, password):
    session.get(pages['home'])
    response = session.post(pages['validate'], params = {
        'sid': username,
        'PIN': password
    }, headers = {
        'referer': pages['login']
    })

    # Check if the login succeeded by looking for a session cookie
    if 'SESSID' not in session.cookies:
        raise InvalidLoginError

def years():
    response = session.get(pages['year'])
    document = BeautifulSoup(response.text)
    for option in (document.find('select', { 'name': 'IN_ACYR' })
                           .find_all('option')):
        yield option['value']

def courses(year):
    response = session.post(pages['course'], params = {
        'IN_ACYR': year,
        'IN_ADLN_OIX': 'X'
    })
    document = BeautifulSoup(response.text)
    for option in (document.find('select', { 'name': 'IN_SUBCRSE' })
                           .find_all('option')):
        yield option['value']

def sections(course, year):
    response = session.post(pages['section'], params = {
        'IN_SUBCRSE': course,
        'IN_PIDM': '',
        'IN_ACYR': year,
        'IN_ADLN_OIX': 'X'
    })
    document = BeautifulSoup(response.text)
    table = document.find('table', { 'class': 'datadisplaytable' })
    rows = table.find_all('tr')
    for row in rows:
        columns = row.find_all('td')
        if len(columns) >= 4:
            yield columns[4].a['href']
