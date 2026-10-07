"""Optional icon regeneration: requires Python and Pillow; not used by the build."""
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parents[1] / 'assets'
out.mkdir(exist_ok=True)
im = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.rounded_rectangle((8, 8, 248, 248), radius=58, fill='#182727')
d.rounded_rectangle((54, 122, 89, 192), radius=15, fill='#46D7A7')
d.rounded_rectangle((109, 83, 144, 192), radius=15, fill='#10A37F')
d.rounded_rectangle((164, 52, 199, 192), radius=15, fill='#6C86FF')
d.ellipse((57, 54, 83, 80), fill='#D3FFF0')
im.save(out / 'TokenMonitor.ico', sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
im.save(out / 'TokenMonitor.png')
