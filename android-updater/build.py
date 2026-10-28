"""Build the small native Android client with the installed Android SDK, without Gradle."""
from pathlib import Path
import argparse,hashlib,json,os,shutil,subprocess,zipfile,xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--work',required=True,type=Path)
parser.add_argument('--output',required=True,type=Path)
parser.add_argument('--sdk',required=True,type=Path)
parser.add_argument('--jdk',required=True,type=Path)
parser.add_argument('--keystore',required=True,type=Path)
parser.add_argument('--password-env',default='REVAMPED_KEYSTORE_PASSWORD')
parser.add_argument('--tests',action='store_true')
args=parser.parse_args()
assert not args.work.exists(),'Choose a new exact build work directory'
assert args.keystore.is_file() and os.environ.get(args.password_env),'Provide the persistent release signing key and password environment variable'
args.work.mkdir(parents=True);args.output.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env.update(TEMP=str(args.work),TMP=str(args.work),JAVA_HOME=str(args.jdk))
jar=args.sdk/'platforms/android-36/android.jar';tools=args.sdk/'build-tools/35.0.0';java=args.jdk/'bin/java.exe'
sha=lambda path:hashlib.file_digest(path.open('rb'),'sha256').hexdigest()

def run(*command):
    subprocess.run([str(item) for item in command],cwd=ROOT,env=env,check=True)

def build(name,manifest,sources,resources=None,assets=None,classpath=None):
    work=args.work/name;work.mkdir();generated=work/'generated';generated.mkdir();classes=work/'classes';classes.mkdir();dex=work/'dex';dex.mkdir()
    package=work/'resources.apk';link=[tools/'aapt2.exe','link','-I',jar,'--manifest',manifest,'--java',generated,'-o',package]
    if resources:
        compiled=work/'compiled.zip';run(tools/'aapt2.exe','compile','--dir',resources,'-o',compiled);link.append(compiled)
    if assets:link.extend(['-A',assets])
    run(*link)
    inputs=list(sources.rglob('*.java'))+list(generated.rglob('*.java'))
    cp=str(jar)+(os.pathsep+str(classpath) if classpath else '')
    run(args.jdk/'bin/javac.exe','-encoding','UTF-8','-source','8','-target','8','-classpath',cp,'-d',classes,*inputs)
    dex_command=[java,'-Djava.io.tmpdir='+str(args.work),'-cp',tools/'lib/d8.jar','com.android.tools.r8.D8','--min-api','26','--lib',jar,'--output',dex]
    if classpath:dex_command.extend(['--classpath',classpath])
    run(*dex_command,*classes.rglob('*.class'))
    unsigned=work/'unsigned.apk';shutil.copyfile(package,unsigned)
    with zipfile.ZipFile(unsigned,'a',compression=zipfile.ZIP_DEFLATED) as archive:
        info=zipfile.ZipInfo('classes.dex',(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;archive.writestr(info,(dex/'classes.dex').read_bytes())
    aligned=work/'aligned.apk';run(tools/'zipalign.exe','-f','4',unsigned,aligned)
    output=args.output/(name+'.apk');assert not output.exists(),'Never overwrite a previously signed deliverable'
    run(java,'-Djava.io.tmpdir='+str(args.work),'-jar',tools/'lib/apksigner.jar','sign','--ks',args.keystore,'--ks-key-alias','revamped-updater','--ks-pass','env:'+args.password_env,'--v4-signing-enabled','false','--out',output,aligned)
    verification=subprocess.check_output([str(java),'-jar',str(tools/'lib/apksigner.jar'),'verify','--verbose','--print-certs',str(output)],env=env,text=True)
    (args.output/(name+'-signature.txt')).write_text(verification,encoding='utf8')
    print(verification,flush=True)
    return output,classes

assets=args.work/'assets';assets.mkdir();shutil.copy2(ROOT.parent/'installer/supported-windows.json',assets/'supported-windows.json')
version=ET.parse(ROOT/'AndroidManifest.xml').getroot().attrib['{http://schemas.android.com/apk/res/android}versionName']
app,classes=build('Revamped-Updater-'+version,ROOT/'AndroidManifest.xml',ROOT/'src',ROOT/'res',assets)
outputs=[app]
if args.tests:
    test,_=build('Revamped-Updater-Tests',ROOT/'tests/AndroidManifest.xml',ROOT/'tests/src',classpath=classes);outputs.append(test)
inputs=[ROOT/'AndroidManifest.xml',ROOT/'build.py',ROOT.parent/'installer/supported-windows.json',*sorted((ROOT/'src').rglob('*')),*sorted((ROOT/'res').rglob('*'))]
receipt={'version':version,'source_files':{p.relative_to(ROOT.parent).as_posix():sha(p) for p in inputs if p.is_file()},'artifacts':{p.name:sha(p) for p in outputs},'sdk_platform':36,'build_tools':'35.0.0','min_sdk':26,'native_sdk_build':True,'signing_verified':True}
(args.output/'BUILD.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf8')
print(json.dumps(receipt['artifacts'],indent=2),flush=True)
