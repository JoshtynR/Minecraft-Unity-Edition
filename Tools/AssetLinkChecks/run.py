"""Read-only GUID and Resources-path audit; Unity package cache is not required."""
import argparse, collections, re, subprocess
from pathlib import Path

root=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--baseline',help='Git revision before an asset move; identify newly lost project GUIDs.')
args=ap.parse_args();errors=[];ids={}
for folder in ['Assets','Packages']:
    for meta in (root/folder).rglob('*.meta'):
        match=re.search(r'^guid: (\S+)',meta.read_text(errors='ignore'),re.M)
        if not match:continue
        if not re.fullmatch(r'[0-9a-f]{32}',match[1]):
            errors.append('Malformed GUID: '+str(meta.relative_to(root))+' '+match[1]);continue
        guid=match[1];path=meta.relative_to(root).as_posix()[:-5]
        if guid in ids:errors.append('Duplicate GUID: '+guid+' '+ids[guid]+' / '+path)
        ids[guid]=path
missing=collections.defaultdict(list)
for path in (root/'Assets').rglob('*'):
    if path.suffix not in ('.asset','.prefab','.unity','.mat','.controller','.anim','.overrideController'):continue
    for guid in set(re.findall(r'guid: ([0-9a-f]{32})',path.read_text(errors='ignore'))):
        if guid not in ids and not guid.startswith('0000000000000000'):
            missing[guid].append(path.relative_to(root).as_posix())
if args.baseline:
    result=subprocess.run(['git','grep','-n','^guid: ',args.baseline,'--','Assets/*.meta','Assets/**/*.meta'],cwd=root,text=True,capture_output=True)
    if result.returncode not in (0,1):raise SystemExit(result.stderr)
    old={}
    for line in result.stdout.splitlines():
        match=re.match(r'[^:]+:(.*\.meta):\d+:guid: (\w+)',line)
        if match:old[match[2]]=match[1][:-5]
    for guid,refs in missing.items():
        if guid in old:errors.append('Lost project asset: '+old[guid]+' -> '+', '.join(refs))
resources=set()
for path in (root/'Assets').rglob('*'):
    if not path.is_file() or path.suffix=='.meta' or '/Resources/' not in path.as_posix():continue
    relative=path.as_posix().split('/Resources/',1)[1]
    resources.add(relative[:-(len(path.suffix))] if path.suffix else relative)
for source in list((root/'Assets/_Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs')):
    text=source.read_text(errors='ignore')
    for key in re.findall(r'Resources\.Load<Material>\("([^"]+)"\)',text):
        if key not in resources:errors.append(str(source.relative_to(root))+': missing material Resources/'+key)
for name in ['Cow','Pig','Sheep','Chicken','Zombie','Skeleton','Creeper','Spider']:
    if 'Materials/'+name+'Material' not in resources:errors.append('Missing mob material '+name)
for key in ['Block Data','Item Data','Entity Data','PlayerHudAssets','UI/Title/widgets']:
    if key not in resources:errors.append('Missing core resource '+key)
for error in errors:print('ERROR:',error)
print(len(ids),'GUIDs indexed;',len(resources),'Resources keys;',len(missing),'GUIDs unresolved without the Unity package cache.')
print('Package/external GUIDs are reported, not treated as proven missing project assets.')
if errors:raise SystemExit(1)
print('PASS: unique GUIDs, required Resources paths'+(' and no referenced project GUIDs lost from '+args.baseline if args.baseline else '')+'.')
