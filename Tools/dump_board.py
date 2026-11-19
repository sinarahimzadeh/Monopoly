import glob, re
from pathlib import Path

guid_map = {}
for p in glob.glob('Assets/_Ropoly/**/*.asset.meta', recursive=True):
    with open(p, 'r', encoding='utf-8') as f:
        for line in f:
            if line.startswith('guid:'):
                guid = line.split(':')[1].strip()
                guid_map[guid] = p[:-5]

board_file = 'Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset'
with open(board_file, 'r', encoding='utf-8') as f:
    text = f.read()

countries_part = text.split('_spaces:')[0]
spaces_part = text.split('_spaces:')[1]

countries = re.findall(r'guid:\s*([a-f0-9]+)', countries_part)
spaces = re.findall(r'guid:\s*([a-f0-9]+)', spaces_part)

print(f'Countries: {len(countries)}')
for c in countries:
    path = guid_map.get(c, 'UNKNOWN')
    print('  Country:', Path(path).stem if path != 'UNKNOWN' else c)

print(f'Spaces: {len(spaces)}')
for i, s in enumerate(spaces):
    path = guid_map.get(s, 'UNKNOWN')
    name = Path(path).stem if path != 'UNKNOWN' else s
    print(f'Tile {i:02d}: {name}')
