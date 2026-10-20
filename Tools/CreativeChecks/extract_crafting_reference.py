"""Extract a recipe fixture from pinned Beta Java registrations, independently of C#.
Usage: python Tools/CreativeChecks/extract_crafting_reference.py /path/to/mc_b1.7.3_release
"""
import csv,json,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
ref=Path(sys.argv[1])/'1.7.3-LTS/src/minecraft/net/minecraft/src'
items=list(csv.DictReader((root/'Tools/CreativeChecks/beta-items.tsv').open(),delimiter='\t'))
item_names={(r['java_field'],int(r['metadata'])):r['item_type'] for r in items}
blocks=json.loads((root/'Tools/BetaCatalog/catalog.json').read_text())['entries']
block_names={(r['sourceId'],r['metadata']):r['name'] for r in blocks}
block_ids={}
# Some declarations have subclasses and chained setup; ID is the first argument.
for name,number in re.findall(r'^\s*(\w+)\s*=.*?new \w+\((\d+)',(ref/'Block.java').read_text(),re.M):block_ids[name]=int(number)
for name,cls in re.findall(r'^\s*(\w+)\s*=.*?new (\w+)\(\)',(ref/'Block.java').read_text(),re.M):
 source=ref/(cls+'.java')
 if source.exists():
  match=re.search(r'super\((\d+)',source.read_text())
  if match:block_ids[name]=int(match[1])
def split(text):
 out=[];start=0;depth=0;quote=None
 for i,c in enumerate(text):
  if quote:
   if c==quote:quote=None
  elif c in '\"\'':quote=c
  elif c=='(':depth+=1
  elif c==')':depth-=1
  elif c==',' and depth==0:out.append(text[start:i].strip());start=i+1
 out.append(text[start:].strip());return out
def value(expr):
 expr=expr.strip();count=1;meta=0
 if expr.startswith('new ItemStack('):
  parts=split(expr[len('new ItemStack('):-1]);expr=parts[0]
  if len(parts)>1:count=int(parts[1])
  if len(parts)>2:meta=int(parts[2])
 if expr.startswith('Item.itemsList['):return 'WhiteWool',count
 if expr.startswith('Item.'):return item_names[expr[5:],meta],count
 if expr.startswith('Block.'):return block_names[block_ids[expr[6:]],meta],count
 raise ValueError(expr)
recipes=[]
def add(output,count,rows,keys):
 width=len(rows[0]);ingredients=[keys.get(c,'Nothing') for row in rows for c in row]
 recipes.append(['shaped',str(width),str(len(rows)),output,str(count),','.join(ingredients)])
# Expand the source's material/pattern product tables rather than the C# registrations.
for filename in ['RecipesTools.java','RecipesWeapons.java','RecipesArmor.java']:
 s=(ref/filename).read_text()
 patterns=[re.findall(r'"([^"]*)"',part) for part in re.search(r'recipePatterns = new String\[\]\[\]\{(.*?)\};',s).group(1).split('}, {')]
 arrays=re.search(r'this.recipeItems = new Object\[\]\[\]\{(.*?)\};',s).group(1)
 arrays=[re.findall(r'(?:Block|Item)\.\w+',part) for part in arrays.split('}, {')]
 for column,material in enumerate(arrays[0]):
  for row,pattern in enumerate(patterns):
   output,count=value(arrays[row+1][column]);add(output,count,pattern,{'X':value(material)[0],'#':'Stick'})
s=(ref/'RecipesIngots.java').read_text()
for block,stack in re.findall(r'\{(Block\.\w+), (new ItemStack\([^)]*\))\}',s):
 out,_=value(block);ingredient,count=value(stack)
 add(out,1,['###','###','###'],{'#':ingredient});add(ingredient,count,['#'],{'#':out})
# All literal registrations, including special-case recipes in the family files.
for filename in ['CraftingManager.java','RecipesTools.java','RecipesWeapons.java','RecipesFood.java','RecipesCrafting.java','RecipesDyes.java']:
 s=(ref/filename).read_text()
 for kind,args in re.findall(r'\.(addRecipe|addShapelessRecipe)\((new ItemStack\([^;]+?)\);',s):
  if re.search(r'\bvar\d+\b',args):continue # material families and wool loop expanded separately
  parts=split(args);output,count=value(parts.pop(0))
  if kind=='addShapelessRecipe':
   recipes.append(['shapeless','0','0',output,str(count),','.join(value(p)[0] for p in parts)])
  else:
   rows=[]
   while parts and parts[0].startswith('"'):rows.append(parts.pop(0)[1:-1])
   keys={parts[i][1:-1]:value(parts[i+1])[0] for i in range(0,len(parts),2)}
   add(output,count,rows,keys)
# RecipesDyes: white-wool + exact dye metadata -> reverse wool metadata.
for metadata in range(16):
 dye=item_names['dyePowder',metadata];wool=block_names[35,15-metadata]
 recipes.append(['shapeless','0','0',wool,'1',dye+',WhiteWool'])
assert len(recipes)==151,len(recipes)
out=root/'Tools/CreativeChecks/beta-crafting.tsv'
with out.open('w') as stream:
 writer=csv.writer(stream,delimiter='\t',lineterminator='\n');writer.writerow(['kind','width','height','output','count','ingredients']);writer.writerows(recipes)
print('Extracted',len(recipes),'Beta recipes from Java source.')
