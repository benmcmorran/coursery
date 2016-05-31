from sqlalchemy import create_engine, MetaData, Table, Column, Integer, String, Float, case, desc, type_coerce, asc, distinct
from sqlalchemy.sql import select, and_, func

engine = create_engine('postgresql://ben@localhost:5432/coursery', convert_unicode=True)
metadata = MetaData(bind=engine)

courses = Table('courses', metadata,
        Column('id', Integer, primary_key=True),
        Column('year', Integer, nullable=False, index=True),
        Column('subject', String(16), nullable=False, index=True),
        Column('course', String(64), nullable=False, index=True),
        Column('section', String(64), nullable=False, index=True),
        Column('instructor', String(256), nullable=False, index=True),
        Column('course_rating', Float, nullable=False),
        Column('instructor_rating', Float, nullable=False),
        Column('workload', Integer, nullable=False),
        Column('grade', String(8), nullable=False),
)

def init_db():
    metadata.create_all()

def recreate_db():
    metadata.drop_all()
    init_db()

def from_json(filename):
    import json
    with open(filename, 'r') as f:
        raw_courses = json.load(f)
    conn = engine.connect()
    for course in raw_courses:
        conn.execute(
                courses.insert(),
                year=course['yr'],
                subject=course['su'],
                course=course['cr'],
                section=course['sc'],
                instructor=course['in'],
                course_rating=course['rc'],
                instructor_rating=course['ri'],
                workload=course['wl'],
                grade=course['gr'],
        )
    conn.close()

AVERAGE="AVERAGE"
ASCENDING='ASCENDING'
DESCENDING='DESCENDING'
keys = {'year', 'subject', 'course', 'section', 'instructor'}

def get_options():
    result = {
        key: [
            r[0] for r in
            select(
                [distinct(courses.columns[key]).label(key)]
            ).order_by(
                asc(courses.columns[key])
            ).execute().fetchall()
        ]
        for key in keys
    }
    return result

def get_courses(order_by='course_rating', order_direction=DESCENDING, limit=100, offset=0, **kwargs):
    numeric_columns = { 'course_rating', 'instructor_rating', 'workload' }
    all_columns = keys | numeric_columns | { 'grade' }

    exact_keys = { key for key in keys if key in kwargs and kwargs[key] != AVERAGE }
    group_keys = keys - kwargs.keys()

    order_by_name = order_by if (order_by in all_columns and kwargs.get(order_by, '') != AVERAGE) else 'course_rating'

    query = select(
            [courses.columns[key] for key in group_keys] +
            [type_coerce(func.avg(courses.columns[key]), Float).label(key) for key in numeric_columns] +
            [type_coerce(func.avg(case(
                { 'A': 4.0, 'B': 3.0, 'C': 2.0, 'NR': 0.0 },
                value=courses.columns.grade,
                else_=0.0,
            )), Float).label('grade')]
        ).where(
            and_(*[courses.columns[key] == kwargs[key] for key in exact_keys])
        ).group_by(
            *[courses.columns[key] for key in group_keys]
        ).order_by(
            desc(order_by_name) if order_direction == DESCENDING else asc(order_by_name)
        ).limit(min(100, max(1, limit))).offset(max(0, offset))

    results = query.execute().fetchall()

    dict_result = []
    for result in results:
        item = dict(result.items())
        for key in exact_keys:
            item[key] = kwargs[key]
        grade = item['grade']
        if grade >= 3.5:
            grade = 'A'
        elif grade >= 2.5:
            grade = 'B'
        elif grade >= 1.5:
            grade = 'C'
        else:
            grade = 'NR'
        item['grade'] = grade
        dict_result.append(item)

    return dict_result
    return query
