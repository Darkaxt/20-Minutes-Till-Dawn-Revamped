using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization.Validation;

public partial class RuntimeChecks
{
 [DataContract]class NativeRouteFile{[DataMember]public string nativeSHA256,scope;[DataMember]public NativeRouteType[] types;}
 [DataContract]class NativeRouteType{[DataMember]public string name,baseType;[DataMember]public bool compilerGenerated,isAbstract,isInterface,isEnum;[DataMember]public NativeRouteMethod[] methods;}
 [DataContract]class NativeRouteMethod{[DataMember]public string id,name,returnType,cil;[DataMember]public int token;[DataMember]public bool isAbstract,isVirtual,isStatic;[DataMember]public string[] parameters,prefixes,postfixes,transpilers;}
 static string RouteType(Type type)=>type==null?null:(type.FullName??type.ToString()).Replace('+','/');
 static string[] RoutePatches(Patches info,string kind)
 {
  if(info==null)return new string[0];var list=kind=="prefix"?info.Prefixes:kind=="postfix"?info.Postfixes:info.Transpilers;
  return list.Where(p=>p.owner==Plugin.Id).Select(p=>RouteType(p.PatchMethod.DeclaringType)+"::"+p.PatchMethod.Name).ToArray();
 }
 void ExportNativeRoutes()
 {
  var assembly=typeof(PlayerController).Assembly;
  var result=new NativeRouteFile{scope="Diagnostic original-method and actual installed Harmony routing inventory. No source-body equivalence or gameplay acceptance claim."};
  using(var stream=File.OpenRead(assembly.Location))using(var hash=SHA256.Create())result.nativeSHA256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
  var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
  result.types=assembly.GetTypes().OrderBy(t=>t.FullName).Select(type=>new NativeRouteType{
   name=RouteType(type),baseType=RouteType(type.BaseType),compilerGenerated=type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),false),isAbstract=type.IsAbstract,isInterface=type.IsInterface,isEnum=type.IsEnum,
   methods=type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)).Distinct().OrderBy(m=>m.MetadataToken).Select(method=>{
    var parameters=method.GetParameters().Select(p=>RouteType(p.ParameterType)).ToArray();var patches=Harmony.GetPatchInfo(method);var body=method.GetMethodBody();
    return new NativeRouteMethod{id=RouteType(type)+"::"+method.Name+"("+string.Join(",",parameters)+")",name=method.Name,returnType=method is MethodInfo info?RouteType(info.ReturnType):"System.Void",token=method.MetadataToken,isAbstract=method.IsAbstract,isVirtual=method.IsVirtual,isStatic=method.IsStatic,parameters=parameters,cil=body==null?null:BitConverter.ToString(body.GetILAsByteArray()).Replace("-","").ToLowerInvariant(),prefixes=RoutePatches(patches,"prefix"),postfixes=RoutePatches(patches,"postfix"),transpilers=RoutePatches(patches,"transpiler")};
   }).ToArray()
  }).ToArray();
  using(var stream=File.Create(Path.Combine(ResultDir,"native-method-routes.json")))new DataContractJsonSerializer(typeof(NativeRouteFile)).WriteObject(stream,result);
  SetPhase("DIAGNOSTIC_COMPLETE");Log("complete","Exact native methods and actual production Harmony targets exported; this is diagnostic routing, not feature validation.");Application.Quit(0);
 }
}
