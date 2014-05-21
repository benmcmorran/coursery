import requests
from bs4 import BeautifulSoup
import os

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

def downloadEvaluations(urlFile):
    with open(urlFile, 'r') as urls:
        i = 1
        directory = 'evaluations'
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
