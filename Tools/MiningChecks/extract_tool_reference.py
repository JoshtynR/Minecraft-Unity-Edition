"""Extract Beta's exact efficiency ID lists from a reference source checkout."""
import argparse,re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('source',type=Path);a=p.parse_args()
s=a.source
blocks=(s/'Block.java').read_text()
ids={name:int(ident) for name,ident in re.findall(r'^        (\w+) = .*?new Block\w*\((\d+)',blocks,re.M)}
lines=['kind\tsource_id']
for kind,cls in [('Pickaxe','ItemPickaxe'),('Axe','ItemAxe'),('Shovel','ItemSpade')]:
 text=(s/(cls+'.java')).read_text()
 names=re.search(r'blocksEffectiveAgainst = new Block\[\]\{(.*?)\}',text).group(1)
 for name in re.findall(r'Block\.(\w+)',names):lines.append(f'{kind}\t{ids[name]}')
Path(__file__).with_name('beta-tool-efficiency.tsv').write_text('\n'.join(lines)+'\n')
