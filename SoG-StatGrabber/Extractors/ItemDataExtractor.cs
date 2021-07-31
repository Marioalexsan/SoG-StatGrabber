using SoG.Modding.Core;
using SoG.Modding.Utils;
using SoG.StatGrabber.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoG.StatGrabber.Extractors
{
    public static class ItemDataExtractor
    {
        public static ConsoleLogger Logger { get; set; } = null;

        private static List<int> idBans = new List<int>
        {
            -1, 2, 3, 4,
            5213,
            10067, 10499, 18000,
            19000, 19001, 19002, 19003, 19100,
            70000, 70001, 70002, 70003, 70004, 70005, 70200,
            170000,
        };

        private static List<string> partialNameBans = new List<string>
        {
            "OBSOLETE",
            "LAST",
            "Hairdo",
            "ArcadiaBlessings",
            "ChaosModeUpgrade",
            "_Icon_",
            "RogueLikeArcadiaReward"
        };

        private static List<EquipmentInfo.StatEnum> statsToGet = new List<EquipmentInfo.StatEnum>
        {
            EquipmentInfo.StatEnum.ATK,
            EquipmentInfo.StatEnum.MATK,
            EquipmentInfo.StatEnum.ASPD,
            EquipmentInfo.StatEnum.CSPD,
            EquipmentInfo.StatEnum.MaxHP,
            EquipmentInfo.StatEnum.MaxEP,
            EquipmentInfo.StatEnum.EPRegen,
            EquipmentInfo.StatEnum.DEF,
            EquipmentInfo.StatEnum.ShldHP,
            EquipmentInfo.StatEnum.Crit,
            EquipmentInfo.StatEnum.CritDMG
        };

        public static void Extract(string argList, int connection)
        {
            //string[] args = Tools.GetArgs(argList);

            Dictionary<object, object> data = new Dictionary<object, object>();

            foreach (ItemCodex.ItemTypes itemID in Enum.GetValues(typeof(ItemCodex.ItemTypes)))
            {
                try
                {
                    string itemName = itemID.ToString();

                    if (idBans.Contains((int)itemID) || partialNameBans.Any(x => itemName.Contains(x)))
                    {
                        continue;
                    }

                    ItemDescription desc = ItemCodex.GetItemDescription(itemID);

                    RetrieveInfo(desc, out ItemCodex.ItemCategories category, out EquipmentInfo info);

                    Dictionary<object, object> itemData = new Dictionary<object, object>();
                    data[itemName] = itemData;

                    itemData["name"] = desc.sFullName;
                    itemData["value"] = desc.iValue;
                    itemData["type"] = category.ToString();

                    if (desc.sDescription != "It's a piece of furniture for your home!")
                    {
                        itemData["lore"] = AdjustItemDescription(itemID, desc.sDescription);
                    }

                    if (desc.iOverrideBloodValue != 0)
                    {
                        itemData["bloodvalue"] = desc.iOverrideBloodValue;
                    }

                    List<object> tags = new List<object>();

                    if (desc.lenCategory.Contains(ItemCodex.ItemCategories.NoDropNoTrade))
                    {
                        tags.Add("NoSelling");
                    }

                    if (tags.Count > 0)
                    {
                        itemData["tags"] = tags;
                    }

                    if (info != null)
                    {
                        Dictionary<object, object> stats = new Dictionary<object, object>();

                        foreach (var stat in statsToGet)
                        {
                            if (info.deniStatChanges.TryGetValue(stat, out int value))
                            {
                                stats[stat.ToString()] = value;
                            }
                        }

                        if (info is WeaponInfo weaponInfo)
                        {
                            string type = weaponInfo.enWeaponCategory == WeaponInfo.WeaponCategory.OneHanded ? "1h" : "2h";
                            type += weaponInfo.enAutoAttackSpell == WeaponInfo.AutoAttackSpell.None ? "" : "-m";

                            stats["ext-type"] = type;
                        }
                        else if (info is HatInfo hatInfo)
                        {
                            stats["ext-type"] = hatInfo.bDoubleSlot ? "mask" : "hat";
                        }

                        if (info.lenSpecialEffects.Count > 0)
                        {
                            stats["special"] = info.lenSpecialEffects.First().ToString();
                        }

                        if (stats.Count > 0)
                        {
                            itemData["stats"] = stats;
                        }
                    }

                    if (desc.lenCategory.Contains(ItemCodex.ItemCategories.PetFood) && PetCodex.denxFoodInfo.TryGetValue(itemID, out var petinfo))
                    {
                        itemData["petfood"] = petinfo.enBonusType.ToString();
                        itemData["petfoodexp"] = petinfo.iBonusValue;
                    }
                }
                catch
                {
                    if (Logger != null)
                    {
                        Logger.Debug("Exception caught for item " + itemID + ". Skipping...");
                    }
                    continue;
                }
            }

            LuaTableFormatter formatter = new LuaTableFormatter()
            {
                IndentSize = 2,
                TableComment = "Generated for Secrets of Grindea\nversion " + APIGlobals.Game.sVersionNumberOnly
            };

            string cooked = formatter.Format(data);

            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream("output.txt", FileMode.Create, FileAccess.Write)))
                {
                    writer.Write(cooked);
                }
                CAS.AddChatMessage("Sent data to output.txt!");
            }
            catch
            {
                CAS.AddChatMessage("Couldn't open file stream for output.txt!");
            }
        }

        private static void RetrieveInfo(ItemDescription desc, out ItemCodex.ItemCategories category, out EquipmentInfo info)
        {
            if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Weapon))
            {
                info = WeaponCodex.GetWeaponInfo(desc.enType);
                category = ItemCodex.ItemCategories.Weapon;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Shield))
            {
                info = EquipmentCodex.GetShieldInfo(desc.enType);
                category = ItemCodex.ItemCategories.Shield;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Armor))
            {
                info = EquipmentCodex.GetArmorInfo(desc.enType);
                category = ItemCodex.ItemCategories.Armor;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Hat))
            {
                info = HatCodex.GetHatInfo(desc.enType);
                category = ItemCodex.ItemCategories.Hat;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Accessory))
            {
                info = EquipmentCodex.GetAccessoryInfo(desc.enType);
                category = ItemCodex.ItemCategories.Accessory;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Shoes))
            {
                info = EquipmentCodex.GetShoesInfo(desc.enType);
                category = ItemCodex.ItemCategories.Shoes;
            }
            else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Facegear))
            {
                info = FacegearCodex.GetHatInfo(desc.enType);
                category = ItemCodex.ItemCategories.Facegear;
            }
            else
            {
                info = null;

                if (desc.lenCategory.Contains(ItemCodex.ItemCategories.KeyItem))
                {
                    category = ItemCodex.ItemCategories.KeyItem;
                }
                else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Usable))
                {
                    category = ItemCodex.ItemCategories.Usable;
                }
                else if (desc.lenCategory.Contains(ItemCodex.ItemCategories.Furniture))
                {
                    category = ItemCodex.ItemCategories.Furniture;
                }
                else
                {
                    category = ItemCodex.ItemCategories.Misc;
                }
            }
        }

        private static string AdjustItemDescription(ItemCodex.ItemTypes type, string target)
        {
            target = target.Replace("[HPPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionHealth_HealthGainedFromPotionInPCT).ToString());
            
            target = target.Replace("[PPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionCrit_Increase).ToString());
            target = target.Replace("[PPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionCrit_BaseDuration).ToString());
            
            target = target.Replace("[DPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionDamage_DMGIncreaseInPCT).ToString());
            target = target.Replace("[DPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionDamage_BaseDuration).ToString());

            target = target.Replace("[EPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionEnergy_EPGainedFromPotionInPCT).ToString());

            target = target.Replace("[ARPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionArrow_ArrowsGained).ToString());

            target = target.Replace("[SPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionSpeed_Increase).ToString());
            target = target.Replace("[SPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionSpeed_BaseDuration).ToString());

            target = target.Replace("[CPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionChicken_BaseDuration).ToString());

            target = target.Replace("[APOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionWealth_GoldIncrease).ToString());
            target = target.Replace("[APOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionWealth_BaseDuration).ToString());

            if (type == ItemCodex.ItemTypes._PotionType_Lightning)
            {
                target = target.Replace("[LPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionLightning_SparksToSpawn).ToString());
                target = target.Replace("[LPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionLightning_SparksToSpawn_OnOtherPotion).ToString());
            }
            else if (type == ItemCodex.ItemTypes._PotionType_Loot)
            {
                target = target.Replace("[LPOT]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionLoot_ChanceIncrease).ToString());
                target = target.Replace("[LPOTS]", SpellVariable.Get(SpellVariable.Handle.Misc_PotionLoot_BaseDuration).ToString());
            }


            return target;
        }
    }
}
