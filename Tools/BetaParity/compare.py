#!/usr/bin/env python3
"""Compare same-stage binary outputs (or BetaParityDump block text files)."""
import argparse
import base64
from collections import Counter
from pathlib import Path
import struct

def load(path):
    if path.suffix == '.txt':
        return base64.b64decode(path.read_text().splitlines()[-1])
    return path.read_bytes()

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('expected',type=Path)
    ap.add_argument('actual',type=Path)
    ap.add_argument('--doubles',action='store_true')
    args=ap.parse_args()
    a,b=load(args.expected),load(args.actual)
    if len(a)!=len(b):raise SystemExit(f'Length mismatch: {len(a)} vs {len(b)}')
    stride=8 if args.doubles else 1
    if len(a)%stride:raise SystemExit('Incomplete double')
    differences=[i for i in range(len(a)//stride) if a[i*stride:(i+1)*stride]!=b[i*stride:(i+1)*stride]]
    print(f'{len(differences)} of {len(a)//stride} values differ.')
    for i in differences[:12]:
        if args.doubles:
            print(f'index={i}: expected={struct.unpack_from("<d",a,i*8)[0]!r}, actual={struct.unpack_from("<d",b,i*8)[0]!r}')
        else:
            print(f'x={i//2048}, y={i%128}, z={(i//128)%16}: expected={a[i]}, actual={b[i]}')
    if not args.doubles:
        print('Most common expected -> actual block pairs:',Counter((a[i],b[i]) for i in differences).most_common(12))
    raise SystemExit(bool(differences))

if __name__=='__main__':main()
