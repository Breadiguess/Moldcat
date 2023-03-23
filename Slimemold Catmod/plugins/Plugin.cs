using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using RWCustom;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "moldcat", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "Bread";

        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("moldcat/super_jump");
        public static readonly PlayerFeature<bool> ExplodeOnDeath = PlayerBool("moldcat/explode_on_death");
        public static readonly GameFeature<float> MeanLizards = GameFloat("moldcat/mean_lizards");
      //public static readonly PlayerFeature<float> CrawlStealth = PlayerFloat("moldcat/crawlstealth");
        public static readonly PlayerFeature<bool> WeakAssArms = PlayerBool("moldcat/WeakAssArms");
        // Add hooks
        public bool IsInit = False;
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Spear.Thrown += Spear_thrown;
            On.Player.Jump += Player_Jump;
            On.Player.Die += Player_Die;
            On.Lizard.ctor += Lizard_ctor;
        }
        
        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
            Futile.atlasManager.LoadAtlas("atlases/moldcathead");
            Futile.atlasManager.LoadAtlas("atlases/moldcatface");
        }

        // Implement MeanLizards
        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if(MeanLizards.TryGet(world.game, out float meanness))
            {
                self.spawnDataEvil = Mathf.Min(self.spawnDataEvil, meanness);
            }
        }


        private void Spear_thrown(On.Spear.Orig_Thrown orig, Spear self, Creature thrownby, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig.Invoke(self, thrownBy, throwPos, firstFrameTraceFromPos, throwDir, frc, eu);
            Player player = thrownBy as Player;
            if(player!= null && Plugin.WeakAssArms.TryGet(player, out bool WeakAssArms) && WeakAssArms)
            {
                self.ChangeMode(Weapon.Mode.Free);
                self.SetRandomSpin();
            }
        }
        // Implement SuperJump
        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);

            if (SuperJump.TryGet(self, out var power))
            {
                self.jumpBoost *= 2f + power;
            }
        }

        // Implement ExlodeOnDeath
        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;

            orig(self);

            if(!wasDead && self.dead
                && ExplodeOnDeath.TryGet(self, out bool explode)
                && explode)
            {
                // Adapted from ScavengerBomb.Explode
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                var color = self.ShortCutColor();
                room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, False));

                room.ScreenMovement(pos, default, 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
            }
        }
    }
}