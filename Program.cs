using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;
using SharpDX;


namespace Vi
{
    class Program
    {
        public static Spell.Chargeable Q;
        public static Spell.Targeted E;
        public static Spell.Targeted R;
        public static Menu Menu, SkillMenu, FarmingMenu, MiscMenu, DrawMenu;
        public static HitChance MinimumHitChance { get; set; }

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Vi")
                return;

            Bootstrap.Init(null);

            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Chargeable(SpellSlot.Q, 100, 860, 4);
            R = new Spell.Targeted(SpellSlot.R, 700);
            E = new Spell.Targeted(SpellSlot.E, 150);
            Menu = MainMenu.AddMenu("Vi", "hellovi");
            Menu.AddSeparator();
            Menu.AddLabel("Created by MyNameIsCool");
            SkillMenu = Menu.AddSubMenu("Skills", "Skills");
            SkillMenu.AddGroupLabel("Skills");
            SkillMenu.AddLabel("Combo");
            SkillMenu.Add("QCombo", new CheckBox("Use Q in Combo"));
            SkillMenu.Add("ECombo", new CheckBox("Use E in Combo"));
            SkillMenu.Add("RCombo", new CheckBox("Use R in Combo"));
            SkillMenu.AddLabel("Harass");
            SkillMenu.Add("EHarass", new CheckBox("Use E on Harass"));
            FarmingMenu = Menu.AddSubMenu("Farming", "Farming");
            FarmingMenu.AddGroupLabel("Farming");
            FarmingMenu.AddLabel("LastHit");
            FarmingMenu.Add("ELH", new CheckBox("Use E to secure last hits", false));
            FarmingMenu.Add("ELHMana", new Slider("Mana Manager for E", 60, 0, 100));
            FarmingMenu.AddLabel("LaneClear");
            FarmingMenu.Add("ELC", new CheckBox("Use E on LaneClear"));
            FarmingMenu.Add("ELCMana", new Slider("Mana Manager for E", 50, 0, 100));
            FarmingMenu.AddLabel("Jungle");
            FarmingMenu.Add("JCQ", new CheckBox("Use Q"));
            FarmingMenu.Add("JCE", new CheckBox("Use E"));
            MiscMenu = Menu.AddSubMenu("Misc", "Misc");
            MiscMenu.AddGroupLabel("Misc");
            MiscMenu.AddLabel("KillSteal");
            MiscMenu.Add("Ekill", new CheckBox("Use E to KillSteal"));
            Game.OnTick += Game_OnTick;
            Chat.Print("Cool Addon Loaded -= Vi =-", System.Drawing.Color.White);
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleFarm();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            KillSteal();
        }
        private static void Combo()
        {
            var useQ = SkillMenu["QCombo"].Cast<CheckBox>().CurrentValue;
            var useR = SkillMenu["RCombo"].Cast<CheckBox>().CurrentValue;
            var useE = SkillMenu["ECombo"].Cast<CheckBox>().CurrentValue;
            if (Q.IsReady() && useQ)
            {
                var target = TargetSelector.GetTarget(Q.MaximumRange, DamageType.Physical);
                var predQ = Q.GetPrediction(target);
                {
                    if (target.IsValidTarget(Q.MaximumRange) && !Q.IsCharging)
                    {
                        Q.StartCharging();
                        return;
                    }

                    if (Q.IsFullyCharged && !target.IsValidTarget(1650))
                    {
                        return;
                    }

                    if (predQ.HitChance >= HitChance.Medium && target.IsInRange(target, 860))
                    {
                        Q.Cast(predQ.CastPosition);
                    }
                }
            }
            if (E.IsReady() && useE)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target.IsValidTarget(E.Range) && !target.IsZombie)
                {
                    E.Cast(target);
                }
            }
            if (R.IsReady()  && useR)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (target.IsValidTarget(R.Range) && !target.IsZombie)
                {
                    R.Cast(target);
                }
            }
        }
        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null) return;
            var useE = MiscMenu["Ekill"].Cast<CheckBox>().CurrentValue;

            if (E.IsReady() && useE && target.IsValidTarget(E.Range) && !target.IsZombie && target.Health <= _Player.GetSpellDamage(target, SpellSlot.E))
            {
                E.Cast(target);
            }
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null) return;
            var useE = SkillMenu["EHarass"].Cast<CheckBox>().CurrentValue;
            if (E.IsReady() && useE && target.IsValidTarget(E.Range) && !target.IsZombie)
            {
                E.Cast(target);
            }
        }

        private static void JungleFarm()
        {
            var useE = FarmingMenu["JCE"].Cast<CheckBox>().CurrentValue;
            var useQ = FarmingMenu["JCQ"].Cast<CheckBox>().CurrentValue;
            //var Wmana = FarmingMenu["ELCMana"].Cast<Slider>().CurrentValue;
            //var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(x => x.MaxHealth).FirstOrDefault(x => x.IsValidTarget(Q.MaximumRange));
            if (Q.IsReady() && useQ)
            {
                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(x => x.MaxHealth).FirstOrDefault(x => x.IsValidTarget(Q.MaximumRange));
                if (target.IsValidTarget(Q.MaximumRange) && !Q.IsCharging)
                {
                    Q.StartCharging();
                    return;
                }

                if (Q.IsFullyCharged && !target.IsValidTarget(1650))
                {
                    return;
                }

                if (target.IsInRange(target, 860))
                {
                    Q.Cast(target);
                }
            }

            if (useE && E.IsReady())
            {
                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(x => x.MaxHealth).FirstOrDefault(x => x.IsValidTarget(E.Range));

                E.Cast(target);
            }
            /*
                        if (useQ && Q.IsReady() && target.IsValidTarget(Q.MaximumRange))
                        {
                            if (!Q.IsCharging)
                                Q.StartCharging();
                            else
                                Q.Cast(target);
                        }*/
        }

        private static void LaneClear()
        {
            var useE = FarmingMenu["ELC"].Cast<CheckBox>().CurrentValue;
            var Wmana = FarmingMenu["ELCMana"].Cast<Slider>().CurrentValue;
            var minions = ObjectManager.Get<Obj_AI_Base>().OrderBy(m => m.Health).Where(m => m.IsMinion && m.IsEnemy && !m.IsDead);
            foreach (var minion in minions)
            {
                if (useE && E.IsReady() && Player.Instance.ManaPercent > Wmana && minion.Health <= _Player.GetSpellDamage(minion, SpellSlot.E))
                {
                    E.Cast(minion);
                }
            }
        }

        private static void LastHit()
        {
            var useW = FarmingMenu["ELH"].Cast<CheckBox>().CurrentValue;
            var Wmana = FarmingMenu["ELHMana"].Cast<Slider>().CurrentValue;
            var minions = ObjectManager.Get<Obj_AI_Base>().OrderBy(m => m.Health).Where(m => m.IsMinion && m.IsEnemy && !m.IsDead);
            foreach (var minion in minions)
            {
                if (useW && E.IsReady() && minion.Health <= _Player.GetSpellDamage(minion, SpellSlot.E))
                {
                    E.Cast(minion);
                }
            }
        }
    }
}