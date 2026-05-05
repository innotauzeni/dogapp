import sys
content = open('tmp_audits.txt', 'r', encoding='utf-8').read()
open('Dog.Portal/Views/Audits/Index.cshtml', 'w', encoding='utf-8').write(content)
print('Written:', len(content.splitlines()), 'lines')
