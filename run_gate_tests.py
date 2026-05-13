"""Smoke tests for the catalog grounding gate.
Each test uses its own isolated temp directory.
"""
import sys, tempfile, json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from uat_runner.runner import _check_catalog_grounding

passed = failed = 0

def ok(name):
    global passed
    sys.stdout.buffer.write(f'PASS: {name}\n'.encode('utf-8'))
    sys.stdout.buffer.flush()
    passed += 1

def fail(name, msg):
    global failed
    sys.stdout.buffer.write(f'FAIL: {name}: {msg}\n'.encode('utf-8'))
    sys.stdout.buffer.flush()
    failed += 1

def run(name, fn):
    try:
        fn()
        ok(name)
    except AssertionError as e:
        fail(name, str(e))
    except Exception as e:
        fail(name, f'{type(e).__name__}: {e}')

ROLE_ENTRY = {'routePath':'/admin/security','role':'SA','elementRole':'Tab',
    'userVisibleLabel':'ROLE MASTER DETAILS','htmlTag':'div','isWatermarked':False}

def make(entries, items):
    """Create isolated temp dir with catalog + checklist; return (cl_path, cat_path)."""
    tmp = tempfile.mkdtemp()
    cat = Path(tmp) / 'catalog.json'
    cl = Path(tmp) / 'checklist.json'
    cat.write_text(json.dumps({'appName':'T','generatedAt':'2026-01-01',
        'schemaVersion':'1.1','entries':entries}), encoding='utf-8')
    cl.write_text(json.dumps({'ProjectName':'T','ReleaseLabel':'T',
        'DocumentDate':'2026-01-01','RequiresAppStateReset':False,
        'Review':[{'FunctionalArea':'T','Subarea':'T','ItemLabel':'T',
                   'Steps':items,'IsJudgmentItem':False,'Passes':False,'Feedback':''}]}),
        encoding='utf-8')
    return cl, cat

def nav(route='/admin/security'):
    return {'StepNumber':1,'Action':f'NAVIGATE TO "http://localhost:5010{route}"','Verifications':[]}

def step(n, action):
    return {'StepNumber':n,'Action':action,'Verifications':[]}

# T1: correct tab name -> no violation
def t1():
    cl, cat = make([ROLE_ENTRY], [nav(), step(2, 'CLICK TAB with name "ROLE MASTER DETAILS"')])
    v = _check_catalog_grounding(cl, cat)
    assert len(v) == 0, f'Expected 0, got {len(v)}'
run('correct label on catalog route -> no violation', t1)

# T2: wrong tab name
def t2():
    cl, cat = make([ROLE_ENTRY], [nav(), step(2, 'CLICK TAB with name "ROLES"')])
    v = _check_catalog_grounding(cl, cat)
    assert len(v) == 1 and v[0].label == 'ROLES', f'Expected 1 violation for ROLES, got {[(x.label,x.route) for x in v]}'
run('fake tab name "ROLES" -> violation', t2)

# T3: uncrawled route -> no violation
def t3():
    cl, cat = make([{'routePath':'/only-known','role':'SA','elementRole':'Button',
        'userVisibleLabel':'SAVE','htmlTag':'button','isWatermarked':False}],
        [nav('/uncrawled'), step(2, 'CLICK BUTTON with caption "Anything"')])
    v = _check_catalog_grounding(cl, cat)
    assert len(v) == 0, f'Expected 0, got {len(v)}'
run('uncrawled route -> no violation (CATALOG_STALE)', t3)

# T4: case-insensitive match
def t4():
    cl, cat = make([ROLE_ENTRY], [nav(), step(2, 'CLICK TAB with name "Role Master Details"')])
    v = _check_catalog_grounding(cl, cat)
    assert len(v) == 0, f'Expected 0, got {len(v)}'
run('case-insensitive label match -> no violation', t4)

# T5: fake link placeholder
def t5():
    cl, cat = make([ROLE_ENTRY], [nav(), step(2, 'CLICK LINK with text "first role row in the grid"')])
    v = _check_catalog_grounding(cl, cat)
    assert len(v) == 1 and v[0].label == 'first role row in the grid', \
        f'Expected 1 violation, got {[(x.label) for x in v]}'
run('fake CLICK LINK placeholder -> violation', t5)

# T6: wrong button caption
def t6():
    entries = [ROLE_ENTRY,
        {'routePath':'/admin/security','role':'SA','elementRole':'Button',
         'userVisibleLabel':'ADD USER-TYPE ROLE','htmlTag':'button','isWatermarked':False}]
    cl_bad, cat = make(entries, [nav(), step(2, 'CLICK BUTTON with caption "Add"')])
    v = _check_catalog_grounding(cl_bad, cat)
    assert len(v) == 1 and v[0].label == 'Add', f'Expected violation for "Add", got {[(x.label) for x in v]}'
run('"Add" button not in catalog -> violation', t6)

# T7: correct button caption
def t7():
    entries = [ROLE_ENTRY,
        {'routePath':'/admin/security','role':'SA','elementRole':'Button',
         'userVisibleLabel':'ADD USER-TYPE ROLE','htmlTag':'button','isWatermarked':False}]
    cl_good, cat = make(entries, [nav(), step(2, 'CLICK BUTTON with caption "ADD USER-TYPE ROLE"')])
    v = _check_catalog_grounding(cl_good, cat)
    assert len(v) == 0, f'Expected 0, got {len(v)}'
run('"ADD USER-TYPE ROLE" in catalog -> no violation', t7)

# T8: multiple items, violations across both
def t8():
    cl_path = tempfile.mktemp(suffix='.json')
    cat_path = tempfile.mktemp(suffix='.json')
    Path(cat_path).write_text(json.dumps({'appName':'T','generatedAt':'2026-01-01',
        'schemaVersion':'1.1','entries':[ROLE_ENTRY]}), encoding='utf-8')
    Path(cl_path).write_text(json.dumps({'ProjectName':'T','ReleaseLabel':'T',
        'DocumentDate':'2026-01-01','RequiresAppStateReset':False,
        'Review':[
            {'FunctionalArea':'A','Subarea':'A','ItemLabel':'Item A',
             'Steps':[nav(), step(2,'CLICK TAB with name "ROLES"')],
             'IsJudgmentItem':False,'Passes':False,'Feedback':''},
            {'FunctionalArea':'B','Subarea':'B','ItemLabel':'Item B',
             'Steps':[nav(), step(2,'CLICK BUTTON with caption "Fake"')],
             'IsJudgmentItem':False,'Passes':False,'Feedback':''},
        ]}), encoding='utf-8')
    v = _check_catalog_grounding(cl_path, cat_path)
    labels = {x.label for x in v}
    assert 'ROLES' in labels and 'Fake' in labels, f'Expected both violations, got {labels}'
run('multiple items each with fake labels -> both violations', t8)

# T9: real broken checklist
def t9():
    checklist_real = Path('docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json')
    catalog_real = Path('docs/component-catalog/index.json')
    v = _check_catalog_grounding(checklist_real, catalog_real)
    labels = {x.label for x in v}
    assert 'ROLES' in labels, f'ROLES not in {labels}'
    assert 'first role row in the grid' in labels, f'fake row label not in {labels}'
    sys.stdout.buffer.write(f'  ({len(v)} violations found)\n'.encode('utf-8'))
    sys.stdout.buffer.flush()
run('real broken checklist catches ROLES + fake row label', t9)

# Summary
sys.stdout.buffer.write(f'\n{passed}/{passed+failed} passed\n'.encode('utf-8'))
sys.stdout.buffer.flush()
sys.exit(0 if failed == 0 else 1)
