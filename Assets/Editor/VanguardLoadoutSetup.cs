using UnityEditor;
using UnityEngine;
using AsterionGame;
public static class VanguardLoadoutSetup {
 static VanguardAbility Skill(VanguardSkill kind,string title,string description,float cooldown,float cast,float range,float damage,float cost=0,float gain=0){
  string path="Assets/Abilities/Vanguard"+kind+".asset";var a=AssetDatabase.LoadAssetAtPath<VanguardAbility>(path);
  if(!a){a=ScriptableObject.CreateInstance<VanguardAbility>();a.skill=kind;a.displayName=title;a.description=description;a.cooldown=cooldown;a.castTime=cast;a.range=range;a.damage=damage;a.energyCost=cost;a.energyOnHit=gain;a.lockMovement=cast>1;AssetDatabase.CreateAsset(a,path);}return a;
 }
 public static void Configure(PlayerClassDefinition vanguard){
  if(vanguard.loadoutVersion>=1)return;
  vanguard.abilities=new AbilityDefinition[]{
   Skill(VanguardSkill.PulseBurst,"Pulse Burst","Drei zielverfolgende Railgun-Schüsse. Je Treffer +4 Gleitenergie und 15% Chance auf eine sofort aufgeladene Overcharge Railgun. 20% Krit-Chance.",0,.6f,22,16,0,4),
   Skill(VanguardSkill.OverchargeRailgun,"Overcharge Railgun","Durchschlägt alle Gegner in einer Linie. Getroffene Gegner erleiden 6 Sekunden lang 15% mehr Schaden von allen Spielern. Proc entfernt Castzeit und Energiekosten; der Cooldown bleibt.",6,1.3f,24,110,30),
   Skill(VanguardSkill.GravitonDash,"Graviton Dash","5,5 Meter in Blickrichtung. Hinterlässt für 4 Sekunden ein Feld: 50% Verlangsamung und Abfangen feindlicher Geschosse. Bricht eigene Casts ab.",9,0,5.5f,0),
   Skill(VanguardSkill.AegisSupplyDrone,"Aegis Supply Drone","Drohne auf den Partner unter der Maus, sonst auf dich. 6 Sekunden Schild in Höhe von 30% max. HP; 4 Sekunden CC-Immunität und Entfernen vorhandener Kontrolleffekte.",16,0,20,0,20),
   Skill(VanguardSkill.ConcussiveBlast,"Concussive Blast","Gezielter Schuss: unterbricht den Gegner und stößt ihn 5 Meter zurück. Eine Kollision mit einer Wand/Barriere betäubt ihn 2 Sekunden.",10,0,22,35,10),
   Skill(VanguardSkill.TractorBeam,"Tractor Beam","Zieht den Partner unter der Maus, sonst den nächsten Partner im Umkreis von 20 Metern zu dir. Wände stoppen den Zug. Ohne Partner kein Cooldown und keine Kosten.",12,0,20,0),
   Skill(VanguardSkill.TargetingMatrix,"Targeting Matrix","Markiert das Ziel 8 Sekunden. Kritische Treffer beider Spieler verkürzen alle ihre Fähigkeiten-Cooldowns um 1 Sekunde und verlängern die Markierung um 1 Sekunde. Markierte Ziele können auch von Partnerangriffen kritisch getroffen werden.",18,0,24,0,15),
   Skill(VanguardSkill.OrbitalKineticStrike,"Orbital Kinetic Strike","Markiert das Zielareal. Nach 1,2 Sekunden: 500 Flächenschaden, entfernt gegnerische Schilde und hinterlässt 6 Sekunden eine Zone mit 30% mehr Schaden für verbündete Spieler.",45,1.5f,24,500,75)
  };
  vanguard.buildEnergyOnHits=true;vanguard.loadoutVersion=1;EditorUtility.SetDirty(vanguard);
 }
}
