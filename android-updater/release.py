"""Package, publish and independently verify the signed Android updater release."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess,urllib.request

ROOT=Path(__file__).resolve().parents[1]
REPO='Darkaxt/20-Minutes-Till-Dawn-Revamped'
VERSION='0.1.0';TAG='updater-v'+VERSION;APK='Revamped-Updater-'+VERSION+'.apk'
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('action',choices=['prepare','publish','verify'])
parser.add_argument('--package',required=True,type=Path)
parser.add_argument('--build',type=Path)
parser.add_argument('--scratch',type=Path)
parser.add_argument('--sdk',type=Path)
parser.add_argument('--jdk',type=Path)
args=parser.parse_args()
sha=lambda path:hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
def git(*command):return subprocess.check_output(['git',*command],cwd=ROOT).decode().strip()
def request(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Revamped-Updater-Release/0.1.0'}))
def read_json(url):
    with request(url) as stream:return json.load(stream)

if args.action=='prepare':
    assert args.build and not args.package.exists(),'Select a build and a new versioned output folder'
    build=json.loads((args.build/'BUILD.json').read_text());assert build['version']==VERSION and build['signing_verified']
    commit=git('rev-parse','HEAD')
    for name,expected in build['source_files'].items():
        assert sha(ROOT/name)==expected,'Changed build source: '+name
        assert hashlib.sha256(subprocess.check_output(['git','show',commit+':'+name],cwd=ROOT)).hexdigest()==expected,'Uncommitted build source: '+name
    assert sha(args.build/APK)==build['artifacts'][APK]
    certificate=re.search(r'Signer #1 certificate SHA-256 digest: ([a-f0-9]{64})',(args.build/(APK.removesuffix('.apk')+'-signature.txt')).read_text()).group(1)
    args.package.mkdir(parents=True);shutil.copyfile(args.build/APK,args.package/APK)
    metadata={'product':'Revamped-Updater','version':VERSION,'version_code':1,'package':'org.darkaxt.revamped.updater','source_commit':commit,'apk_sha256':sha(args.package/APK),'certificate_sha256':certificate,'source_files':build['source_files'],'min_sdk':26,'target_sdk':36}
    (args.package/'UPDATER.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf8')
    (args.package/'SHA256SUMS.txt').write_text(''.join(sha(args.package/name)+'  '+name+'\n' for name in [APK,'UPDATER.json']),encoding='utf8')
    print(json.dumps(metadata,indent=2))
elif args.action=='publish':
    metadata=json.loads((args.package/'UPDATER.json').read_text());commit=metadata['source_commit']
    assert git('ls-remote','origin','refs/heads/main').split()[0]==commit,'Push the verified source before release'
    assert sha(args.package/APK)==metadata['apk_sha256']
    subprocess.run(['gh','release','create',TAG,*[str(args.package/name) for name in [APK,'UPDATER.json','SHA256SUMS.txt']],'--repo',REPO,'--target',commit,'--title','Revamped Updater '+VERSION,'--notes-file',str(ROOT/'release/NOTES-updater-0.1.0.md'),'--latest=false'],cwd=ROOT,check=True)
else:
    assert args.scratch and not args.scratch.exists() and args.sdk and args.jdk,'Provide new verification scratch and installed SDK/JDK'
    expected=json.loads((args.package/'UPDATER.json').read_text())
    release=read_json('https://api.github.com/repos/'+REPO+'/releases/tags/'+TAG)
    assert not release['draft'] and not release['prerelease'] and release['published_at'] and release['tag_name']==TAG
    ref=read_json('https://api.github.com/repos/'+REPO+'/git/ref/tags/'+TAG)
    assert ref['object']['type']=='commit' and ref['object']['sha']==expected['source_commit']
    assets={row['name']:row for row in release['assets']};assert set(assets)=={APK,'UPDATER.json','SHA256SUMS.txt'}
    args.scratch.mkdir(parents=True)
    for name,row in assets.items():
        assert row['state']=='uploaded' and row['digest'].startswith('sha256:')
        with request(row['browser_download_url']) as stream,(args.scratch/name).open('wb') as output:shutil.copyfileobj(stream,output)
        assert sha(args.scratch/name)==row['digest'][7:]==sha(args.package/name),name
    assert json.loads((args.scratch/'UPDATER.json').read_text())==expected
    for row in (args.scratch/'SHA256SUMS.txt').read_text().splitlines():
        digest,name=row.split('  ');assert name in {APK,'UPDATER.json'} and sha(args.scratch/name)==digest
    java=args.jdk/'bin/java.exe';tools=args.sdk/'build-tools/35.0.0'
    signing=subprocess.check_output([str(java),'-jar',str(tools/'lib/apksigner.jar'),'verify','--verbose','--print-certs',str(args.scratch/APK)],text=True)
    assert 'Signer #1 certificate SHA-256 digest: '+expected['certificate_sha256'] in signing
    badging=subprocess.check_output([str(tools/'aapt2.exe'),'dump','badging',str(args.scratch/APK)],text=True)
    assert "package: name='org.darkaxt.revamped.updater' versionCode='1' versionName='0.1.0'" in badging
    (args.package/'delivery-verified.json').write_text(json.dumps({'tag':TAG,'url':release['html_url'],'published_at':release['published_at'],'source_commit':expected['source_commit'],'apk_sha256':expected['apk_sha256'],'certificate_sha256':expected['certificate_sha256'],'normal_published_release':True,'independently_downloaded_assets':sorted(assets),'signature_and_package_verified':True},indent=2)+'\n',encoding='utf8')
    print((args.package/'delivery-verified.json').read_text())
