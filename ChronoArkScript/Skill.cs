// Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Skill
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChronoArkMod;
using GameDataEditor;
using UnityEngine;

public class Skill
{
	public CharInfoSkillData CharinfoSkilldata;

	public bool FreeUse;

	public bool ForceAction;

	public bool IsNowCasting;

	public bool TeamAttack;

	public bool NoAttackTimeWait;

	public bool Enforce;

	public bool Enforce_Weak;

	public bool Enforce_CantUse;

	public bool NeverCri;

	public BattleChar Master;

	public GDESkillData MySkill;

	public BattleTeam MyTeam;

	public Sprite SkillExtendedImage;

	public string UseOtherParticle_Path;

	public List<Skill_Extended> AllExtendeds = new List<Skill_Extended>();

	public int UsedApNum = -99;

	public Skill OriginalSelectSkill;

	public object DelObj;

	public bool WaitButtonFlag;

	public SkillButton MyButton;

	public int _AP;

	public int APChange;

	public int AutoDelete;

	private bool _Disposable;

	private bool _Fatal;

	private bool _NotCount;

	private bool _NoExchange;

	private bool _BasicOption;

	private bool _Track;

	public bool _IgnoreTaunt;

	public bool NotAvailable;

	public bool IsWaste;

	public bool BasicSkill;

	public BasicSkill BasicSkillButton;

	public bool PlusHit;

	public int _Counting;

	private int UseingCounting;

	private bool isCounting;

	public bool isExcept;

	public string Image_Skill;

	public string Image_Button;

	public string Image_Basic;

	public bool IsCreatedInBattle
	{
		get
		{
			bool result = false;
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.SetNonCreatedInBattle)
				{
					return false;
				}
			}
			if (BattleSystem.instance != null && BattleSystem.instance.BattleInitialSkillsData.Any())
			{
				result = ((!(CharinfoSkilldata != null) || !BattleSystem.instance.BattleInitialSkillsData.Contains(CharinfoSkilldata)) ? true : false);
			}
			return result;
		}
	}

	public bool CanUseStun
	{
		get
		{
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.CanUseStun)
				{
					return true;
				}
			}
			return false;
		}
	}

	public string TargetTypeKey
	{
		get
		{
			string result = MySkill.Target.Key;
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (!string.IsNullOrEmpty(allExtended.ChangedTarget))
				{
					result = allExtended.ChangedTarget;
				}
			}
			return result;
		}
	}

	public bool GetIsSkillinHand
	{
		get
		{
			if (BattleSystem.instance != null)
			{
				foreach (Skill skill in BattleSystem.instance.AllyTeam.Skills)
				{
					if (skill == this)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool GetIsSkillinDeck
	{
		get
		{
			if (BattleSystem.instance != null)
			{
				foreach (Skill item in BattleSystem.instance.AllyTeam.Skills_Deck)
				{
					if (item == this)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool GetIsSkillinUsedDeck
	{
		get
		{
			if (BattleSystem.instance != null)
			{
				foreach (Skill item in BattleSystem.instance.AllyTeam.Skills_UsedDeck)
				{
					if (item == this)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public int TargetDamageOriginal
	{
		get
		{
			int num = BattleChar.CalculationResult(Master.GetStat.atk, MySkill.Effect_Target.DMG_Per, MySkill.Effect_Target.DMG_Base);
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public int TargetDamage
	{
		get
		{
			int num = BattleChar.CalculationResult(Master.GetStat.atk, MySkill.Effect_Target.DMG_Per + PlusSkillPerStat.Damage, MySkill.Effect_Target.DMG_Base + PlusSkillBaseStat.Target_BaseDMG);
			num += (int)Misc.PerToNum(num, PlusSkillPerFinal.Damage);
			num += PlusSkillBaseFinal.Target_BaseDMG;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public int TargetDamage_OnlyFinal => (int)Misc.PerToNum(BattleChar.CalculationResult(Master.GetStat.atk, MySkill.Effect_Target.DMG_Per + PlusSkillPerStat.Damage, MySkill.Effect_Target.DMG_Base + PlusSkillBaseStat.Target_BaseDMG), PlusSkillPerFinal.Damage) + PlusSkillBaseFinal.Target_BaseDMG;

	public int TargetDamageView
	{
		get
		{
			int num = BattleChar.CalculationResult(Master.GetStat.atk, MySkill.Effect_Target.DMG_Per + PlusSkillPerStat.Damage, MySkill.Effect_Target.DMG_Base + PlusSkillBaseStat.Target_BaseDMG + PlusSkillBaseStatPreview.Target_BaseDMG);
			num += (int)Misc.PerToNum(num, PlusSkillPerFinal.Damage);
			num += PlusSkillBaseFinal.Target_BaseDMG;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public int TargetHealOriginal
	{
		get
		{
			int num = BattleChar.CalculationResult(Master.GetStat.reg, MySkill.Effect_Target.HEAL_Per, MySkill.Effect_Target.HEAL_Base + (int)Misc.PerToNum(Master.GetStat.maxhp, MySkill.Effect_Target.HEAL_MaxHpPer));
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public int TargetHeal
	{
		get
		{
			int num = BattleChar.CalculationResult(Master.GetStat.reg, MySkill.Effect_Target.HEAL_Per + PlusSkillPerStat.Heal, MySkill.Effect_Target.HEAL_Base + PlusSkillBaseStat.Target_BaseHeal);
			num += (int)Misc.PerToNum(num, PlusSkillPerFinal.Heal);
			num += PlusSkillBaseFinal.Target_BaseHeal;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public int TargetHeal_OnlyFinal => (int)Misc.PerToNum(BattleChar.CalculationResult(Master.GetStat.reg, MySkill.Effect_Target.HEAL_Per + PlusSkillPerStat.Heal, MySkill.Effect_Target.HEAL_Base + PlusSkillBaseStat.Target_BaseHeal), PlusSkillPerFinal.Heal) + PlusSkillBaseFinal.Target_BaseHeal;

	public bool TargetForceHeal
	{
		get
		{
			if (MySkill.Effect_Target.ForceHeal)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.SkillBasePlus.TargetForceHeal)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool TargetChainHeal
	{
		get
		{
			if (MySkill.Effect_Target.ChainHeal)
			{
				return true;
			}
			return false;
		}
	}

	public bool CounterEnable
	{
		get
		{
			if (MySkill.Effect_Target.DMG_Base != 0 || MySkill.Effect_Target.DMG_Per != 0)
			{
				return true;
			}
			return false;
		}
	}

	public List<string> ChoiceSkillListCheck
	{
		get
		{
			List<string> list = new List<string>();
			if (MySkill.Target.Key == GDEItemKeys.s_targettype_choiceskill)
			{
				foreach (Skill_Extended allExtended in AllExtendeds)
				{
					if (allExtended.ChoiceSkillList == null)
					{
						continue;
					}
					foreach (string choiceSkill in allExtended.ChoiceSkillList)
					{
						list.Add(choiceSkill);
					}
				}
			}
			return list;
		}
	}

	public List<Skill> ChoiceSkillList
	{
		get
		{
			List<Skill> list = new List<Skill>();
			if (MySkill.Target.Key == GDEItemKeys.s_targettype_choiceskill)
			{
				foreach (Skill_Extended allExtended in AllExtendeds)
				{
					if (allExtended.ChoiceSkillList == null)
					{
						continue;
					}
					foreach (string choiceSkill in allExtended.ChoiceSkillList)
					{
						Skill skill = TempSkill(choiceSkill, Master, Master.MyTeam);
						skill._Counting += _Counting;
						skill.BasicSkill = BasicSkill;
						list.Add(skill);
					}
				}
			}
			return list;
		}
	}

	public int AP
	{
		get
		{
			int num = _AP;
			if (NotCount)
			{
				num = _AP;
			}
			else if (Master != null)
			{
				int num2 = Master.Overload;
				if (num2 <= 0)
				{
					num2 = 0;
				}
				num += num2;
			}
			if (BasicSkill)
			{
				num++;
			}
			if (BattleSystem.instance != null)
			{
				if (IsNowCounting)
				{
					if (UsedApNum < 0)
					{
						UsedApNum = 0;
					}
					return UsedApNum;
				}
				if (BasicSkill)
				{
					num += Master.GetStat.PlusMPUse.PlusMP_Fixed;
				}
				else
				{
					num += Master.GetStat.PlusMPUse.PlusMP_Skills;
					int plusMP_OnlyHand = Master.GetStat.PlusMPUse.PlusMP_OnlyHand;
					if (plusMP_OnlyHand != 0 && GetIsSkillinHand)
					{
						num += plusMP_OnlyHand;
					}
					else
					{
						int plusMP_Deck = Master.GetStat.PlusMPUse.PlusMP_Deck;
						if (plusMP_Deck != 0 && GetIsSkillinDeck)
						{
							num += plusMP_Deck;
						}
						else
						{
							int plusMP_UsedDeck = Master.GetStat.PlusMPUse.PlusMP_UsedDeck;
							if (plusMP_UsedDeck != 0 && GetIsSkillinUsedDeck)
							{
								num += plusMP_UsedDeck;
							}
						}
					}
				}
			}
			num += APChange;
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				num += allExtended.APChange;
			}
			if (num < 0)
			{
				num = 0;
			}
			if (MyButton != null && MyButton.isActiveAndEnabled)
			{
				int num3 = MySkill.UseAp;
				if (BasicSkill)
				{
					num3++;
				}
				if (MyButton.gameObject.activeSelf)
				{
					if (num3 > num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", 1);
					}
					else if (num3 < num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", -1);
					}
					else if (num3 == num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", 0);
					}
				}
			}
			if (BasicSkillButton != null && BasicSkillButton.isActiveAndEnabled)
			{
				int num4 = MySkill.UseAp;
				if (BasicSkill)
				{
					num4++;
				}
				if (BasicSkillButton.gameObject.activeSelf)
				{
					if (num4 > num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", 1);
					}
					else if (num4 < num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", -1);
					}
					else if (num4 == num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", 0);
					}
				}
			}
			return num;
		}
		set
		{
			_AP = value;
		}
	}

	public int AP_OverloadViewOnly
	{
		get
		{
			if (BattleSystem.instance == null)
			{
				return AP;
			}
			int num = _AP;
			if (NotCount)
			{
				num = _AP;
			}
			else if (Master != null)
			{
				int num2 = Master.Overload;
				if (num2 <= 0)
				{
					num2 = 0;
				}
				num += num2;
			}
			if (BasicSkill)
			{
				num++;
			}
			if (BattleSystem.instance != null)
			{
				if (IsNowCounting)
				{
					if (UsedApNum < 0)
					{
						UsedApNum = 0;
					}
					return UsedApNum;
				}
				PlusMP plusMPUse = Master.GetStat.PlusMPUse;
				if (BasicSkill)
				{
					num += plusMPUse.PlusMP_Fixed;
				}
				else
				{
					num += plusMPUse.PlusMP_Skills;
					int plusMP_OnlyHand = plusMPUse.PlusMP_OnlyHand;
					if (plusMP_OnlyHand != 0 && GetIsSkillinHand)
					{
						num += plusMP_OnlyHand;
					}
					else
					{
						int plusMP_Deck = plusMPUse.PlusMP_Deck;
						if (plusMP_Deck != 0 && GetIsSkillinDeck)
						{
							num += plusMP_Deck;
						}
						else
						{
							int plusMP_UsedDeck = plusMPUse.PlusMP_UsedDeck;
							if (plusMP_UsedDeck != 0 && GetIsSkillinUsedDeck)
							{
								num += plusMP_UsedDeck;
							}
						}
					}
				}
				if (BattleSystem.instance.SelectedSkill != null && BattleSystem.instance.TargetSelecting)
				{
					Skill skill = BattleSystem.instance.SelectedSkill;
					if (skill.OriginalSelectSkill != null)
					{
						skill = skill.OriginalSelectSkill;
					}
					if (skill.Master == Master && skill != this)
					{
						if (SaveManager.NowData.GameOptions.Difficulty == 1)
						{
							if (Master is BattleAlly && (Master as BattleAlly).IsLucy)
							{
								num++;
							}
						}
						else if (!skill.NotCount && !NotCount)
						{
							num++;
						}
					}
				}
			}
			num += APChange;
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				num += allExtended.APChange;
			}
			if (num < 0)
			{
				num = 0;
			}
			if (MyButton != null && MyButton.isActiveAndEnabled)
			{
				int num3 = MySkill.UseAp;
				if (BasicSkill)
				{
					num3++;
				}
				if (MyButton.gameObject.activeSelf)
				{
					if (num3 > num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", 1);
					}
					else if (num3 < num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", -1);
					}
					else if (num3 == num)
					{
						MyButton.MPAni.SetInteger("MPSTAT", 0);
					}
				}
			}
			if (BasicSkillButton != null && BasicSkillButton.isActiveAndEnabled)
			{
				int num4 = MySkill.UseAp;
				if (BasicSkill)
				{
					num4++;
				}
				if (BasicSkillButton.gameObject.activeSelf)
				{
					if (num4 > num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", 1);
					}
					else if (num4 < num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", -1);
					}
					else if (num4 == num)
					{
						BasicSkillButton.MPAni.SetInteger("MPSTAT", 0);
					}
				}
			}
			return num;
		}
	}

	public bool Disposable
	{
		get
		{
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.SetNonDisposable)
				{
					return false;
				}
			}
			if (_Disposable)
			{
				return true;
			}
			foreach (Skill_Extended allExtended2 in AllExtendeds)
			{
				if (allExtended2.Disposable)
				{
					return true;
				}
			}
			return false;
		}
		set
		{
			_Disposable = value;
		}
	}

	public bool Fatal
	{
		get
		{
			if (_Fatal)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.Fatal)
				{
					return true;
				}
			}
			return false;
		}
		set
		{
			_Fatal = value;
		}
	}

	public bool NotCount
	{
		get
		{
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.SetNonQuick)
				{
					return false;
				}
			}
			if (_NotCount)
			{
				return true;
			}
			foreach (Skill_Extended allExtended2 in AllExtendeds)
			{
				if (allExtended2.NotCount)
				{
					return true;
				}
			}
			return false;
		}
		set
		{
			_NotCount = value;
		}
	}

	public bool NoExchange
	{
		get
		{
			if (_NoExchange)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.NoExchange)
				{
					return true;
				}
			}
			return false;
		}
		set
		{
			_NoExchange = value;
		}
	}

	public bool BasicOption
	{
		get
		{
			if (_BasicOption)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.BasicOption)
				{
					return true;
				}
			}
			return false;
		}
		set
		{
			_BasicOption = value;
		}
	}

	public bool Track
	{
		get
		{
			if (SaveManager.Difficalty != 2)
			{
				return false;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.Traking)
				{
					return true;
				}
			}
			return _Track;
		}
		set
		{
			_Track = value;
		}
	}

	public bool IgnoreTaunt
	{
		get
		{
			if (Master.GetStat.IgnoreTaunt)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.IgnoreTaunt)
				{
					return true;
				}
			}
			if ((MySkill.Target.Key == GDEItemKeys.s_targettype_enemy || MySkill.Target.Key == GDEItemKeys.s_targettype_enemy_PlusRandom) && Master.BuffFind(GDEItemKeys.Buff_B_DarkCross))
			{
				return true;
			}
			return _IgnoreTaunt;
		}
		set
		{
			_IgnoreTaunt = value;
		}
	}

	public bool IsNowCounting
	{
		get
		{
			if (BattleSystem.instance != null)
			{
				foreach (CastingSkill castSkill in BattleSystem.instance.CastSkills)
				{
					if (castSkill.skill == this)
					{
						return true;
					}
				}
				foreach (CastingSkill item in BattleSystem.instance.SaveSkill)
				{
					if (item.skill == this)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public int Counting
	{
		get
		{
			if (isCounting)
			{
				return UseingCounting;
			}
			int num = _Counting;
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				num += allExtended.Counting;
			}
			if (num <= 0)
			{
				return 0;
			}
			return num;
		}
		set
		{
			_Counting = value;
		}
	}

	public bool IsDamage
	{
		get
		{
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.IsDamage)
				{
					return true;
				}
			}
			if (MySkill.Effect_Target.DMG_Per >= 1 || MySkill.Effect_Target.DMG_Base >= 1)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsHeal
	{
		get
		{
			if (MySkill.HealSkill)
			{
				return true;
			}
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (allExtended.IsHeal)
				{
					return true;
				}
			}
			if (MySkill.Effect_Target.HEAL_Per >= 1 || MySkill.Effect_Target.HEAL_Base >= 1)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsVFX => MySkill.VFX;

	public bool IsTargetSkill
	{
		get
		{
			if (MySkill.Target.Key == GDEItemKeys.s_targettype_allskill || MySkill.Target.Key == GDEItemKeys.s_targettype_Misc || MySkill.Target.Key == GDEItemKeys.s_targettype_skill || MySkill.Target.Key == GDEItemKeys.s_targettype_choiceskill)
			{
				return false;
			}
			return true;
		}
	}

	public Stat PlusSkillStat
	{
		get
		{
			Stat result = new Stat();
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				result += allExtended.PlusSkillStat;
			}
			return result;
		}
	}

	public PerStat PlusSkillPerStat
	{
		get
		{
			PerStat result = default(PerStat);
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (IsDamage)
				{
					result.Damage += allExtended.PlusSkillPerStat.Damage;
				}
				if (IsHeal)
				{
					result.Heal += allExtended.PlusSkillPerStat.Heal;
				}
				result.MaxHP += allExtended.PlusSkillPerStat.MaxHP;
			}
			if (PlayData.TSavedata.SpRule is SR_BattlePriest)
			{
				int heal = result.Heal;
				result.Heal = result.Damage;
				result.Damage = heal;
			}
			return result;
		}
	}

	public PerStat PlusSkillPerFinal
	{
		get
		{
			PerStat result = default(PerStat);
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (IsDamage)
				{
					result.Damage += allExtended.PlusSkillPerFinal.Damage;
				}
				if (IsHeal)
				{
					result.Heal += allExtended.PlusSkillPerFinal.Heal;
				}
			}
			if (PlayData.TSavedata.SpRule is SR_BattlePriest)
			{
				int heal = result.Heal;
				result.Heal = result.Damage;
				result.Damage = heal;
			}
			return result;
		}
	}

	public SkillBasestat PlusSkillBaseFinal
	{
		get
		{
			SkillBasestat result = default(SkillBasestat);
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (IsDamage)
				{
					result.Target_BaseDMG += allExtended.SkillBaseFinal.Target_BaseDMG;
				}
				if (IsHeal)
				{
					result.Target_BaseHeal += allExtended.SkillBaseFinal.Target_BaseHeal;
				}
			}
			if (PlayData.TSavedata.SpRule is SR_BattlePriest)
			{
				int target_BaseHeal = result.Target_BaseHeal;
				result.Target_BaseHeal = result.Target_BaseDMG;
				result.Target_BaseDMG = target_BaseHeal;
			}
			return result;
		}
	}

	public SkillBasestat PlusSkillBaseStat
	{
		get
		{
			SkillBasestat result = default(SkillBasestat);
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (IsDamage)
				{
					result.Target_BaseDMG += allExtended.SkillBasePlus.Target_BaseDMG;
					result.Target_BaseDMG += (int)Misc.PerToNum(TargetDamageOriginal, allExtended.PlusPerStat.Damage);
				}
				if (IsHeal)
				{
					result.Target_BaseHeal += allExtended.SkillBasePlus.Target_BaseHeal;
					result.Target_BaseHeal += (int)Misc.PerToNum(TargetHealOriginal, allExtended.PlusPerStat.Heal);
				}
			}
			if (PlayData.TSavedata.SpRule is SR_BattlePriest)
			{
				int target_BaseHeal = result.Target_BaseHeal;
				result.Target_BaseHeal = result.Target_BaseDMG;
				result.Target_BaseDMG = target_BaseHeal;
			}
			return result;
		}
	}

	public SkillBasestat PlusSkillBaseStatPreview
	{
		get
		{
			SkillBasestat result = default(SkillBasestat);
			foreach (Skill_Extended allExtended in AllExtendeds)
			{
				if (IsDamage)
				{
					result.Target_BaseDMG += allExtended.SkillBasePlusPreview.Target_BaseDMG;
				}
				if (IsHeal)
				{
					result.Target_BaseHeal += allExtended.SkillBasePlusPreview.Target_BaseHeal;
				}
			}
			if (PlayData.TSavedata.SpRule is SR_BattlePriest)
			{
				int target_BaseHeal = result.Target_BaseHeal;
				result.Target_BaseHeal = result.Target_BaseDMG;
				result.Target_BaseDMG = target_BaseHeal;
			}
			return result;
		}
	}

	public List<T> IReturn<T>() where T : class
	{
		List<T> list = new List<T>();
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			if (allExtended as T != null)
			{
				list.Add(allExtended as T);
			}
		}
		return list;
	}

	public List<BuffTag> ReturnAllBuffs()
	{
		List<BuffTag> list = new List<BuffTag>();
		List<BuffTag> list2 = new List<BuffTag>();
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			list.AddRange(allExtended.TargetBuff);
			list2.AddRange(allExtended.SelfBuff);
		}
		list = Buff.BuffsSort(MySkill.Effect_Target, list);
		list2 = Buff.BuffsSort(MySkill.Effect_Self, list2);
		List<BuffTag> list3 = new List<BuffTag>();
		list3.AddRange(list);
		list3.AddRange(list2);
		return list3;
	}

	public List<BuffTag> ReturnTargetBuffs()
	{
		List<BuffTag> list = new List<BuffTag>();
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			list.AddRange(allExtended.TargetBuff);
		}
		return Buff.BuffsSort(MySkill.Effect_Target, list);
	}

	public int GetCriPer(BattleChar Target, int Plus = 0)
	{
		float num = 0f;
		num = ((!(Target != null)) ? (Master.GetStat.cri + (float)Plus) : ((!IsHeal) ? ((float)Target.GetStat.crihit + Master.GetStat.cri + (float)Plus) : (Master.GetStat.cri + (float)Plus)));
		if (BattleSystem.instance != null)
		{
			foreach (IP_CriPerChange item in BattleSystem.instance.IReturn<IP_CriPerChange>())
			{
				item?.CriPerChange(this, Target, ref num);
			}
		}
		if (Fatal)
		{
			num *= 2f;
		}
		num += (float)MySkill.Effect_Target.CRI + PlusSkillStat.cri;
		return (int)num;
	}

	public void UseCountSkill()
	{
		UseingCounting = Counting;
		isCounting = true;
	}

	public void Remove()
	{
		if (MyButton != null)
		{
			MyButton.WasteButton.SetActive(value: false);
			MyButton.transform.parent.tag = "Untagged";
			DeleteData(Useskill: false);
			BattleSystem.instance.ActWindow.Window.GetSkillData(MyTeam);
			MyButton.Remove();
		}
		else if (OriginalSelectSkill != null)
		{
			OriginalSelectSkill.Delete();
		}
	}

	public void Except()
	{
		isExcept = true;
		MyButton.Waste();
	}

	public void Delete(bool ClickWaste = false)
	{
		if (MyButton != null)
		{
			if (ClickWaste)
			{
				MyButton.UseWaste();
			}
			else
			{
				MyButton.Waste();
			}
		}
		else if (OriginalSelectSkill != null)
		{
			OriginalSelectSkill.Delete(ClickWaste);
		}
	}

	public void DeleteData(bool Useskill)
	{
		if (MyButton.IsNowCasting)
		{
			return;
		}
		for (int i = 0; i < MyTeam.Skills.Count; i++)
		{
			if (MyTeam.Skills[i] == this)
			{
				MyTeam.Skills.RemoveAt(i);
				break;
			}
		}
		if (Useskill && !Disposable && !isExcept)
		{
			BattleSystem.instance.ActWindow.Skillwin.WasteLightMove(MyButton.MainButton.Trashbutton.transform, MyButton.Myskill);
			Skill skill = ExtendedDelete();
			if (skill != null)
			{
				MyTeam.Skills_UsedDeck.Insert(0, skill);
				for (int j = 0; j < skill.AllExtendeds.Count; j++)
				{
					skill.AllExtendeds[j].UsedDeckInit();
				}
			}
		}
		else if (!Useskill && !isExcept)
		{
			if (MyButton != null)
			{
				BattleSystem.instance.ActWindow.Skillwin.WasteLightMove(MyButton.MainButton.Trashbutton.transform, MyButton.Myskill);
			}
			Skill skill2 = ExtendedDelete();
			if (skill2 != null)
			{
				MyTeam.Skills_UsedDeck.Insert(0, skill2);
				for (int k = 0; k < skill2.AllExtendeds.Count; k++)
				{
					skill2.AllExtendeds[k].UsedDeckInit();
				}
			}
		}
		else if (!MyButton.Cuteffect)
		{
			BattleSystem.instance.StartCoroutine(Delay(Useskill));
		}
	}

	public IEnumerator Delay(bool Use)
	{
		if (!Use)
		{
			yield return new WaitForSecondsRealtime(0.5f);
		}
		else
		{
			yield return new WaitForSecondsRealtime(0.2f);
		}
		if (MyButton != null && MyButton.gameObject != null)
		{
			MyButton.OneTimeUseeffect.transform.SetParent(UIManager.inst.MainCanvas.transform);
			MyButton.OneTimeUseeffect.SetActive(value: true);
		}
	}

	public Skill ExtendedDelete()
	{
		Skill skill = new Skill();
		skill.Init(CharinfoSkilldata, Master, MyTeam);
		Skill skill2 = skill.CloneSkill();
		skill2.CharinfoSkilldata.CopyData(skill);
		for (int i = 0; i < skill2.AllExtendeds.Count; i++)
		{
			if (skill2.AllExtendeds[i].Data != null && skill2.AllExtendeds[i].Data.IsOnesInit)
			{
				skill2.AllExtendeds.RemoveAt(i);
				i--;
			}
		}
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			if (allExtended.Data != null && allExtended.Data.IsOnesInit)
			{
				skill2.AllExtendeds.Add(Skill_Extended.ExtendedClone(Master, allExtended, skill2));
			}
			else if (allExtended.BattleExtended)
			{
				Skill_Extended skill_Extended = allExtended.Clone() as Skill_Extended;
				allExtended.WhenAnotherClone_BattleExOnly(skill_Extended);
				skill2.ExtendedAdd_Battle(skill_Extended);
			}
		}
		return skill2;
	}

	public void ExtendedDelete(string ExtendedTypeName)
	{
		for (int i = 0; i < AllExtendeds.Count; i++)
		{
			if (AllExtendeds[i].Name == ExtendedTypeName)
			{
				AllExtendeds[i].SelfDestroy();
				break;
			}
		}
	}

	public void ExtendedDelete_Dataname(string GDEExtendedName)
	{
		for (int i = 0; i < AllExtendeds.Count; i++)
		{
			if (AllExtendeds[i].Data != null && AllExtendeds[i].Data.Key == GDEExtendedName)
			{
				AllExtendeds[i].SelfDestroy();
				break;
			}
		}
	}

	public void ExtendedDelete<T>()
	{
		for (int i = 0; i < AllExtendeds.Count; i++)
		{
			if (AllExtendeds[i] is T)
			{
				AllExtendeds[i].SelfDestroy();
				break;
			}
		}
	}

	public static bool GetLock(string SkillKey)
	{
		GDESkillData gDESkillData = new GDESkillData(SkillKey);
		if (gDESkillData.NoDrop)
		{
			return false;
		}
		if (gDESkillData.Lock)
		{
			foreach (string unlockItem in SaveManager.NowData.unlockList.UnlockItems)
			{
				if (gDESkillData.Key == unlockItem)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static List<Skill> CharToSkills(BattleChar Input, BattleTeam Team)
	{
		List<Skill> list = new List<Skill>();
		for (int i = 0; i < Input.Info.SkillDatas.Count; i++)
		{
			if (Input.Info.BasicSkill != Input.Info.SkillDatas[i])
			{
				Skill skill = new Skill();
				skill.Init(Input.Info.SkillDatas[i], Input, Team);
				Skill skill2 = skill.CloneSkill(Perfect: false, null, skill.AllExtendeds);
				skill2.CharinfoSkilldata.CopyData(skill);
				list.Add(skill2);
			}
		}
		return list;
	}

	public void Init(CharInfoSkillData SkillData, BattleChar Input = null, BattleTeam Team = null)
	{
		Init(SkillData.SkillInfo, Input, Team);
		if (SkillData.SKillExtended != null)
		{
			ExtendedAdd_Battle(SkillData.SKillExtended);
		}
		CharinfoSkilldata = SkillData;
	}

	public void Init(GDESkillData Skilldata, BattleChar Input = null, BattleTeam Team = null)
	{
		Master = Input;
		if (Input == null && BattleSystem.instance != null)
		{
			Master = BattleSystem.instance.DummyChar;
		}
		if (Team == null)
		{
			if (BattleSystem.instance != null)
			{
				MyTeam = BattleSystem.instance.AllyTeam;
			}
		}
		else
		{
			MyTeam = Team;
		}
		initField(Skilldata);
	}

	public void initField(GDESkillData Skilldata)
	{
		MySkill = Skilldata;
		AP = Skilldata.UseAp;
		AutoDelete = Skilldata.AutoDelete;
		NotCount = Skilldata.NotCount;
		NoExchange = Skilldata.NotChuck;
		isExcept = Skilldata.Except;
		Counting = Skilldata.Counting;
		BasicOption = Skilldata.Basic;
		Fatal = Skilldata.Fatal;
		Disposable = Skilldata.Disposable;
		IgnoreTaunt = Skilldata.IgnoreTaunt;
		Track = Skilldata.Track;
		if (!string.IsNullOrEmpty(Skilldata.Image_0_Path) && string.IsNullOrEmpty(Image_Skill))
		{
			Image_Skill = Skilldata.Image_0_Path;
		}
		if (!string.IsNullOrEmpty(Skilldata.Image_1_Path) && string.IsNullOrEmpty(Image_Button))
		{
			Image_Button = Skilldata.Image_1_Path;
		}
		if (!string.IsNullOrEmpty(Skilldata.Image_2_Path) && string.IsNullOrEmpty(Image_Basic))
		{
			Image_Basic = Skilldata.Image_2_Path;
		}
		if (CharinfoSkilldata == null)
		{
			CharinfoSkilldata = new CharInfoSkillData(Skilldata);
		}
	}

	public static Skill TempSkill(string Key, BattleChar input = null, BattleTeam team = null)
	{
		Skill skill = new Skill();
		if (team == null && input != null)
		{
			skill.MyTeam = input.MyTeam;
		}
		else
		{
			skill.MyTeam = team;
		}
		skill.Init(new GDESkillData(Key), input, team);
		return skill.CloneSkill();
	}

	public static Skill TempSkill(string Key)
	{
		Skill skill = new Skill();
		skill.initField(new GDESkillData(Key));
		return skill;
	}

	public static Skill FieldInfoSkill(string Key, Character Input, BattleTeam Team)
	{
		BattleAlly battleAlly = new BattleAlly();
		battleAlly.Info = Input;
		new Skill();
		Skill skill = TempSkill(Key, battleAlly, Team);
		skill.Master = battleAlly;
		return skill;
	}

	public Skill CloneSkill(bool Perfect = false, BattleChar User = null, List<Skill_Extended> Enforcelist = null, bool NewCharinfo = false)
	{
		Skill skill = new Skill();
		if (User == null)
		{
			skill.Init(MySkill.ShallowClone(), Master, MyTeam);
		}
		else
		{
			skill.Init(MySkill.ShallowClone(), User, MyTeam);
		}
		skill.Enforce_Weak = false;
		skill.Enforce = false;
		skill.Enforce_CantUse = false;
		skill.UsedApNum = -99;
		skill.NotCount = _NotCount;
		skill.NoExchange = _NoExchange;
		skill.AP = _AP;
		skill.SkillExtendedImage = SkillExtendedImage;
		skill.BasicSkill = BasicSkill;
		skill._Counting = _Counting;
		skill.Disposable = _Disposable;
		skill.isExcept = isExcept;
		skill.BasicOption = _BasicOption;
		skill.CharinfoSkilldata = CharinfoSkilldata;
		skill.AutoDelete = AutoDelete;
		if (skill.MySkill.KeyID == "")
		{
			skill.MySkill.KeyID = MySkill.Key;
		}
		if (MySkill.Effect_Target.Key != "null")
		{
			skill.MySkill.Effect_Target = MySkill.Effect_Target.ShallowClone();
		}
		if (MySkill.Effect_Self.Key != "null")
		{
			skill.MySkill.Effect_Self = MySkill.Effect_Self.ShallowClone();
		}
		skill.Image_Skill = Image_Skill;
		skill.Image_Button = Image_Button;
		skill.Image_Basic = Image_Basic;
		skill.Enforce_Weak = false;
		skill.Enforce = false;
		skill.Enforce_CantUse = false;
		if (skill.MySkill.KeyID == GDEItemKeys.Skill_S_SacrificeSkill)
		{
			skill.Enforce_Weak = true;
		}
		if (!Perfect)
		{
			skill.CharinfoSkilldata = new CharInfoSkillData(skill.MySkill);
			if (Enforcelist != null)
			{
				foreach (Skill_Extended item in Enforcelist)
				{
					if (item.Data != null)
					{
						skill.ExtendedAdd_Battle(Skill_Extended.DataToExtended(item.Data));
						if (item.Data.Key == GDEItemKeys.SkillExtended_Cantuse_Ex)
						{
							skill.Enforce_CantUse = true;
						}
					}
				}
			}
			if (skill.MySkill.SkillExtended != null)
			{
				foreach (string i in skill.MySkill.SkillExtended)
				{
					if (skill.AllExtendeds.Find((Skill_Extended a) => a.Data != null && a.Data.Key == i) == null)
					{
						Skill_Extended skill_Extended = Skill_Extended.DataToExtendedC(i);
						skill_Extended.isDataExtended = true;
						skill.ExtendedAdd(skill_Extended);
					}
				}
			}
			if (skill.MySkill.SKillExtendedItem != null)
			{
				foreach (GDESkillExtendedData i2 in skill.MySkill.SKillExtendedItem)
				{
					if (i2.IsOnesInit && AllExtendeds != null && AllExtendeds.Count != 0)
					{
						Skill_Extended skill_Extended2 = AllExtendeds.Find((Skill_Extended a) => a.Data != null && a.Data.Key == i2.Key);
						if (skill_Extended2 != null)
						{
							Skill_Extended skill_Extended3 = Skill_Extended.ExtendedClone(User, skill_Extended2, skill);
							skill_Extended3.isDataExtended = true;
							skill.AllExtendeds.Add(skill_Extended3);
						}
						else
						{
							Skill_Extended skill_Extended4 = Skill_Extended.DataToExtended(i2);
							skill_Extended4.isDataExtended = true;
							skill.ExtendedAdd(skill_Extended4);
						}
					}
					else
					{
						skill.ExtendedAdd(Skill_Extended.DataToExtended(i2));
					}
				}
			}
			if (Master.Info.Ally)
			{
				if (PlayData.TSavedata.SpRule != null)
				{
					PlayData.TSavedata.SpRule.AllSkillAddExtended(skill);
				}
				if (BattleSystem.instance != null)
				{
					foreach (IP_AllSkillAddExtended item2 in skill.Master.IReturn<IP_AllSkillAddExtended>())
					{
						item2?.AllskilladdExtended(skill);
					}
				}
			}
			foreach (Skill_Extended allExtended in skill.AllExtendeds)
			{
				if (allExtended.fail)
				{
					Debug.LogError("스킬 이름: " + skill.MySkill.Name);
				}
			}
		}
		else
		{
			skill.AllExtendeds = new List<Skill_Extended>();
			foreach (Skill_Extended allExtended2 in AllExtendeds)
			{
				if (!allExtended2.isDestroy && !allExtended2.NoClone)
				{
					skill.ExtendedAdd(Skill_Extended.ExtendedClone(User, allExtended2, skill));
				}
			}
			skill.Enforce_Weak = Enforce_Weak;
			skill.Enforce = Enforce;
			skill.Enforce_CantUse = Enforce_CantUse;
			foreach (Skill_Extended allExtended3 in AllExtendeds)
			{
				if (allExtended3.Data != null && allExtended3.Data.Drop)
				{
					if (allExtended3.Data.Debuff)
					{
						skill.Enforce_Weak = true;
					}
					else
					{
						skill.Enforce = true;
					}
				}
			}
		}
		if (skill.MySkill.User == "LucyCurse")
		{
			skill.Enforce_Weak = true;
		}
		if (skill.CharinfoSkilldata == null)
		{
			skill.CharinfoSkilldata = new CharInfoSkillData(skill.MySkill);
		}
		if (NewCharinfo && skill.CharinfoSkilldata != null)
		{
			CharInfoSkillData charinfoSkilldata = skill.CharinfoSkilldata;
			CharInfoSkillData charInfoSkillData = new CharInfoSkillData(skill.MySkill);
			if (charinfoSkilldata.SKillExtended != null)
			{
				charInfoSkillData.SKillExtended = (Skill_Extended)charinfoSkilldata.SKillExtended.Clone();
			}
			charInfoSkillData.SkillInfo = skill.MySkill;
			skill.CharinfoSkilldata = charInfoSkillData;
		}
		return skill;
	}

	public void CloneOther_AllExtendeds(Skill TargetSkill, bool NoCloneData = true)
	{
		if (TargetSkill == null || TargetSkill.MySkill == null)
		{
			return;
		}
		Enforce_Weak = TargetSkill.Enforce_Weak;
		Enforce = TargetSkill.Enforce;
		Enforce_CantUse = TargetSkill.Enforce_CantUse;
		foreach (Skill_Extended allExtended in TargetSkill.AllExtendeds)
		{
			if (allExtended == null || allExtended.Data == null || string.IsNullOrEmpty(allExtended.Data.Key) || allExtended.isDestroy || allExtended.NoClone || (NoCloneData && allExtended.isDataExtended))
			{
				continue;
			}
			AllExtendeds.Add(allExtended);
			if (allExtended.Data.Drop)
			{
				if (allExtended.Data.Debuff)
				{
					Enforce_Weak = true;
				}
				else
				{
					Enforce = true;
				}
			}
		}
	}

	public Skill_Extended ExtendedAdd(string gdeExtended)
	{
		Skill_Extended skill_Extended = Skill_Extended.DataToExtended(gdeExtended);
		AllExtendeds.Add(ExtendedInit(skill_Extended, Battle: false));
		return skill_Extended;
	}

	public Skill_Extended ExtendedAdd(Skill_Extended Extended)
	{
		AllExtendeds.Add(ExtendedInit(Extended, Battle: false));
		return Extended;
	}

	public Skill_Extended ExtendedAdd_Battle(Skill_Extended Extended)
	{
		AllExtendeds.Add(ExtendedInit(Extended, Battle: true));
		return Extended;
	}

	public Skill_Extended ExtendedAdd_Battle(string gdeExtended)
	{
		Skill_Extended skill_Extended = Skill_Extended.DataToExtended(gdeExtended);
		AllExtendeds.Add(ExtendedInit(skill_Extended, Battle: true));
		return skill_Extended;
	}

	public Skill_Extended ExtendedInit(Skill_Extended Extended, bool Battle)
	{
		if (Master != null)
		{
			Extended.MyChar = Master.Info;
		}
		Extended.MySkill = this;
		Extended.Init();
		if (Extended.Data != null && Extended.Data.Drop)
		{
			if (!Extended.Data.Debuff)
			{
				Enforce = true;
			}
			else
			{
				Enforce_Weak = true;
			}
		}
		if (!Extended.IsInited)
		{
			Extended.IsInited = true;
			Extended.BattleExtended = Battle;
		}
		if (Extended.SpriteImage != null)
		{
			BattleSystem.instance.EffectDelays.Enqueue(BattleSystem.instance.ExtendedSkillAddDelay(Extended, this));
		}
		return Extended;
	}

	public Skill_Extended ExtendedFind_DataName(string GDEExName)
	{
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			if (allExtended.Data != null && allExtended.Data.Key == GDEExName)
			{
				return allExtended;
			}
		}
		return null;
	}

	public Skill_Extended ExtendedFind(string ExtendedName, bool NoError = true)
	{
		Type type = ModManager.GetType(ExtendedName);
		if (type == null)
		{
			if (!NoError)
			{
				Debug.LogError(ExtendedName + ": Not Found (Skill Extended)");
			}
			return null;
		}
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			if (allExtended.Name == type.Name)
			{
				return allExtended;
			}
		}
		return null;
	}

	public T ExtendedFind<T>() where T : Skill_Extended
	{
		foreach (Skill_Extended allExtended in AllExtendeds)
		{
			if (allExtended is T)
			{
				return (T)allExtended;
			}
		}
		return null;
	}

	public bool DeleteCheck()
	{
		if (AutoDelete >= 1)
		{
			AutoDelete--;
			if (AutoDelete <= 0)
			{
				Delete();
				return true;
			}
		}
		return false;
	}
}
