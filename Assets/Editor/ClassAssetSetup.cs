using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AsterionGame;
[InitializeOnLoad]
public static class ClassAssetSetup {
 const string Root="Assets/Resources/Classes";
 static bool queued;
 static ClassAssetSetup(){Queue();EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)Queue();};}
 public static void Queue(){if(queued)return;queued=true;EditorApplication.delayCall+=Ensure;}
 static T Asset<T>(string path,Action<T> initialize) where T:ScriptableObject {
  var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
  asset=ScriptableObject.CreateInstance<T>();asset.name=Path.GetFileNameWithoutExtension(path);initialize(asset);AssetDatabase.CreateAsset(asset,path);return asset;
 }
 static void Ensure(){
  queued=false;if(EditorApplication.isPlayingOrWillChangePlaymode)return;
  if(EditorApplication.isCompiling || EditorApplication.isUpdating){Queue();return;}
  Directory.CreateDirectory(Root);AssetDatabase.Refresh();
  var laser=Asset<LaserAbility>("Assets/Abilities/TwinPulses.asset",a=>{a.displayName="Twin Pulses";a.description="1 Sekunde Cast. Zwei zielverfolgende Schüsse kurz nacheinander. 20% Chance auf Overcharge: F sofort, ohne eigenen Cooldown und mit 50% mehr Schaden.";a.cooldown=0;a.castTime=1;a.twinProjectiles=true;a.range=19;a.damage=10;a.energyOnHit=3;a.beamColor=new Color(1,.56f,.15f);});
  var dash=Asset<DashAbility>("Assets/Abilities/JetBurst.asset",a=>{a.displayName="Jet Burst";a.description="Kurzer Jetpack-Schub in Bewegungsrichtung.";a.cooldown=3.2f;a.range=5.6f;a.jetAssisted=true;a.energyCost=0;});
  var grenade=Asset<GrenadeAbility>("Assets/Abilities/HunterGrenade.asset",a=>{a.displayName="Ion Grenade";a.description="Wirft eine Granate auf den Mauspunkt. Treffer erzeugen 12 Energie pro Gegner.";a.cooldown=5f;a.range=13f;a.damage=110f;a.radius=2.6f;a.energyOnHit=12f;});
  var kick=Asset<KickAbility>("Assets/Abilities/PulseKick.asset",a=>{a.displayName="Pulse Kick";a.description="Kurzer Vorstoß mit Kick. Betäubt getroffene Gegner für 2 Sekunden. +8 Energie pro Treffer.";a.cooldown=7f;a.range=2.1f;a.damage=35f;a.stunDuration=2f;a.dashDistance=2.1f;a.energyOnHit=8f;});
  var repulsor=Asset<GrenadeAbility>("Assets/Abilities/RepulsorGrenade.asset",a=>{a.displayName="Repulsor Grenade";a.description="Wirft eine Granate nach vorne. Drückt Gegner zurück; Wände stoppen den Rückstoß. +8 Energie.";a.cooldown=9f;a.range=5f;a.damage=30f;a.radius=2.8f;a.forwardThrow=true;a.knockbackDistance=4f;a.energyOnHit=8f;});
  var charged=Asset<ChargedShotAbility>("Assets/Abilities/ChargedShot.asset",a=>{a.displayName="Charged Shot";a.description="Lädt 1,1 Sekunden im Stand auf. Mit Overcharge sofort, ohne eigenen Cooldown und mit 50% mehr Schaden. +15 Energie bei Treffer.";a.cooldown=7f;a.castTime=1.1f;a.range=24f;a.damage=180f;a.energyOnHit=15f;a.lockMovement=true;});
  var shield=Asset<ShieldAbility>("Assets/Abilities/AegisShield.asset",a=>{a.displayName="Aegis Shield";a.description="Verringert eingehenden Schaden für 3 Sekunden um 25 Prozent.";a.cooldown=10f;a.duration=3f;a.reduction=0.25f;a.energyCost=15f;});
  var ultimate=Asset<UltimateAbility>("Assets/Abilities/Overdrive.asset",a=>{a.displayName="Overdrive";a.description="Kanalisiert 4 Sekunden einen verheerenden Strahl auf dein Ziel. Jetpack (2) bricht ab.";a.cooldown=35f;a.range=24f;a.damage=80f;a.energyFraction=0.75f;a.channelDuration=4f;a.channelTickInterval=0.2f;a.lockMovement=true;});
  var vanguard=Asset<PlayerClassDefinition>(Root+"/01_Vanguard.asset",d=>{d.classId="vanguard";d.displayName="Vanguard";d.role="STURMSOLDAT / ENERGIEGEWEHR";d.description="Standfest. Präzise. Schwer bewaffnet.\nBrich mit deinem Energiegewehr die Verteidigung des Wächters und rufe einen Orbitalangriff herab.";d.accent=new Color(.2f,.78f,1);d.maximumHealth=120;d.moveSpeed=6.3f;d.abilities=new AbilityDefinition[]{AssetDatabase.LoadAssetAtPath<LaserAbility>("Assets/Abilities/PulseLaser.asset"),AssetDatabase.LoadAssetAtPath<DashAbility>("Assets/Abilities/PhaseShift.asset"),AssetDatabase.LoadAssetAtPath<OrbitalAbility>("Assets/Abilities/OrbitalStrike.asset")};});
  var hunter=Asset<PlayerClassDefinition>(Root+"/02_BountyHunter.asset",d=>{d.classId="bounty_hunter";d.displayName="Bounty Hunter";d.role="KOPFGELDJÄGER / ZWEI PISTOLEN";d.description="Zwei Pistolen. Ein Jetpack. Keine Ruhepause.\nBleibe in Bewegung, setze schnelle Treffer und entkomme Gefahren mit deinem Jet-Schub.";d.accent=new Color(1,.56f,.22f);d.maximumHealth=100;d.moveSpeed=7;d.jetpack=true;d.abilities=new AbilityDefinition[]{laser,dash,grenade};});
  if(hunter.loadoutVersion<2){hunter.abilities=new AbilityDefinition[]{laser,dash,grenade,kick,repulsor,charged,shield,ultimate};hunter.buildEnergyOnHits=true;hunter.loadoutVersion=2;laser.energyOnHit=3;dash.energyCost=0;EditorUtility.SetDirty(hunter);EditorUtility.SetDirty(laser);EditorUtility.SetDirty(dash);}
  if(hunter.loadoutVersion<3){
   hunter.gameplayVisualScale=1.2f;hunter.loadoutVersion=3;laser.twinProjectiles=true;laser.cooldown=.28f;
   laser.description="1 Sekunde Cast. Zwei zielverfolgende Schüsse kurz nacheinander. 20% Chance auf Overcharge: F sofort, ohne eigenen Cooldown und mit 50% mehr Schaden.";
   EditorUtility.SetDirty(hunter);EditorUtility.SetDirty(laser);
  }
  if(hunter.loadoutVersion<4){
   hunter.loadoutVersion=4;laser.cooldown=0;laser.castTime=1;laser.twinProjectiles=true;
   laser.description="1 Sekunde Cast. Zwei zielverfolgende Schüsse kurz nacheinander. 20% Chance auf Overcharge: F sofort, ohne eigenen Cooldown und mit 50% mehr Schaden.";
   grenade.cooldown=10;grenade.energyCost=25;grenade.energyOnHit=0;grenade.damage=15;grenade.dotDuration=15;grenade.dotDamage=6;grenade.slowFraction=.5f;
   grenade.description="Fliegt zum ausgewählten Ziel. 15 Flächenschaden, danach 15 Sekunden lang 6 Schaden pro Sekunde und 50% Bewegungsverlangsamung. Kostet 25 Energie.";
   EditorUtility.SetDirty(hunter);EditorUtility.SetDirty(laser);EditorUtility.SetDirty(grenade);
  }
  if(hunter.loadoutVersion<5){
   hunter.loadoutVersion=5;laser.description="1 Sekunde Cast. Zwei zielverfolgende Schüsse kurz nacheinander. 20% Chance auf Overcharge: F sofort, ohne eigenen Cooldown und mit 50% mehr Schaden.";charged.description="Lädt 1,1 Sekunden im Stand auf. Mit Overcharge sofort, ohne eigenen Cooldown und mit 50% mehr Schaden. +15 Energie bei Treffer.";ultimate.description="Kanalisiert 4 Sekunden einen verheerenden Strahl auf dein Ziel. Jetpack (2) bricht ab.";
   EditorUtility.SetDirty(hunter);EditorUtility.SetDirty(laser);EditorUtility.SetDirty(charged);EditorUtility.SetDirty(ultimate);
  }
  HunterLoadoutSetup.Configure(hunter);
  VanguardLoadoutSetup.Configure(vanguard);
  Bind(vanguard,"Vanguard");Bind(hunter,"BountyHunter");AssetDatabase.SaveAssets();
 }
 static void Bind(PlayerClassDefinition definition,string modelName){
  var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/"+modelName+".fbx");
  if(modelName=="Vanguard"){var sentinel=AssetDatabase.LoadAssetAtPath<GameObject>(VanguardMeshySetup.PrefabPath);if(sentinel)model=sentinel;}
  if(modelName=="BountyHunter"){var meshy=AssetDatabase.LoadAssetAtPath<GameObject>(MeshyHunterSetup.PrefabPath);if(meshy)model=meshy;}
  if(definition.model==model)return;definition.model=model;EditorUtility.SetDirty(definition);
 }
}
public sealed class ClassModelRefresh : AssetPostprocessor {
 static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom){
  foreach(string path in imported)if(path=="Assets/Art/Models/BountyHunter.fbx" || path=="Assets/Art/Models/Vanguard.fbx"){ClassAssetSetup.Queue();break;}
 }
}
