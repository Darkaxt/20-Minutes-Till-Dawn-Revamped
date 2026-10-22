using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 void VerifyTitleArtwork()
 {
  Require(GameObject.Find("RevampedTitleCredit")==null,"footer substitute is removed");
  var titleCanvas=GameObject.Find("Canvas/TitleScreen");var promotion=titleCanvas==null?null:titleCanvas.transform.Find("EmberpathBanner");
  Require(promotion!=null&&!promotion.gameObject.activeInHierarchy,"complete native promotion, wishlist and link target are inactive");
  Require(promotion.GetComponentsInChildren<UnityEngine.UI.Button>(true).All(b=>!b.IsActive()),"promotion leaves no active invisible clickable button");
  Require(TitleArtwork.Instance.Logos.Count>0,"owned logo artwork is modified");
  foreach(var logo in TitleArtwork.Instance.Logos)
  {
   Require(logo.Sprite.vertices.Take(logo.OriginalVertices.Length).SequenceEqual(logo.OriginalVertices),"all original logo/authorship vertices remain exact");
   Require(logo.Sprite.triangles.Take(logo.OriginalTriangles.Length).SequenceEqual(logo.OriginalTriangles),"all original logo/authorship triangles remain exact");
   Require(ColorfulStyle.PixelHash(logo.Sprite.texture)==logo.OriginalTextureHash,"original source artwork remains untouched");
   var sampled=ColorfulStyle.Sample(logo.Artwork.texture);int changed=0;
   try
   {
    var actual=sampled.GetPixels32();
    for(int i=0;i<actual.Length;i++)if(!actual[i].Equals(logo.OriginalPixels[i]))
    {
     Require(logo.OriginalPixels[i].a==0&&actual[i].a==255,"only previously transparent artwork pixels become credit ink");changed++;
    }
   }
   finally{Object.Destroy(sampled);}
   Require(changed==logo.CreditPixels,"exactly the pixel-letter mask changes; all original logo/authorship pixels remain exact");
   Require(logo.CreditPixels>200&&Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>().Any(i=>i.gameObject.activeInHierarchy&&i.sprite==logo.Artwork),"visible native logo uses the modified pixel artwork without tight-UV clipping");
  }
 }
 IEnumerator CheckTitleArtwork(TitleScreenController title)
 {
  while(TitleArtwork.Instance==null||TitleArtwork.Instance.Logos.Count==0||title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  foreach(var image in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>().Where(i=>i.sprite!=null&&i.sprite.texture.name.Contains("20Logo")))Log("artwork-consumer","Image "+image.name+" mesh="+image.useSpriteMesh+" sprite="+image.sprite.name+" rect="+image.sprite.rect+" textureRect="+image.sprite.textureRect+" active="+image.gameObject.activeInHierarchy);
  foreach(var renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>().Where(i=>i.sprite!=null&&i.sprite.texture.name.Contains("20Logo")))Log("artwork-consumer","SpriteRenderer "+renderer.name+" sprite="+renderer.sprite.name+" rect="+renderer.sprite.rect+" textureRect="+renderer.sprite.textureRect+" active="+renderer.gameObject.activeInHierarchy);
  foreach(var image in Resources.FindObjectsOfTypeAll<UnityEngine.UI.RawImage>().Where(i=>i.texture!=null&&i.texture.name.Contains("20Logo")))Log("artwork-consumer","RawImage "+image.name+" uv="+image.uvRect);
  VerifyTitleArtwork();yield return Capture("pixel-art-title.png");
  var sprite=TitleArtwork.Instance.Logos.First().Sprite;int vertices=sprite.vertices.Length;
  TitleArtwork.Instance.ApplyLoaded();Require(sprite.vertices.Length==vertices,"reapplying artwork does not duplicate letters");
  title.ChangeState<OptionsMenuState>();while(!Active(title.optionsMenu))yield return null;
  yield return Capture("pixel-art-options.png");
  title.ChangeState<TitleMainMenuState>();while(!Active(title.mainMenu)||title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  VerifyTitleArtwork();yield return Capture("pixel-art-title-return.png");
  SetPhase("PASS");Log("complete","Actual native pixel-art logo credit, original geometry/pixels, no footer, Options/title and idempotence verified");Application.Quit(0);
 }
}
