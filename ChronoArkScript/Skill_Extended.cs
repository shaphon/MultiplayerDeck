// Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Skill_Extended
using System;
using System.Collections;
using System.Collections.Generic;
using ChronoArkMod;
using GameDataEditor;
using UnityEngine;

public class Skill_Extended : Passive_Char, ICloneable
{
	public bool CanMultyStack;

	public bool EnemyTargetAIOnly;

	public bool fail;

	public bool EnemyPreviewNoArrow;

	public bool NoClone;

	public bool CanUseStun;

	public bool BattleExtended;

	public bool IsInited;

	public bool IsButtonInited;

	public bool CountingExtedned;

	public int Counting;

	public Skill MySkill;

	public GDESkillExtendedData Data;

	private string _Des;

	public Sprite SpriteImage;

	public GameObject SkillParticleLive;

	public string SkillParticleObject;

	public bool SkillParticleNoTooltip;

	public int APChange;

	public List<BuffTag> TargetBuff = new List<BuffTag>();

	public List<BuffTag> SelfBuff = new List<BuffTag>();

	public int PartyBarrier;

	public bool ParticleOn;

	public bool IsDamage;

	public bool IsHeal;

	public Stat PlusSkillStat = new Stat();

	public PerStat PlusSkillPerStat;

	public SkillBasestat SkillBasePlus;

	public SkillBasestat SkillBasePlusPreview;

	public PerStat PlusSkillPerFinal;

	public SkillBasestat SkillBaseFinal;

	public bool Fatal;

	public bool NotCount;

	public bool NoExchange;

	public bool BasicOption;

	public bool IgnoreTaunt;

	public bool Traking;

	public bool Disposable;

	public bool SetNonDisposable;

	public bool SetNonCreatedInBattle;

	public bool SetNonQuick;

	public bool isDestroy;

	public bool isDataExtended;

	public GameObject BuffIcon;

	public int BuffIconStackNum;

	public List<string> ChoiceSkillList;

	public bool DefuiltTerms = true;

	public string ChangedTarget;

	public bool SetAlwaysCanUse;

	public string Name => GetType().Name;

	public override BattleChar BChar => MySkill.Master;

	public virtual string ExtendedDes()
	{
		return _Des;
	}

	public virtual string ExtendedName()
	{
		return Data.Name;
	}

	public virtual bool CanIgnoreTauntTarget(BattleChar IgnoreTauntTarget)
	{
		return false;
	}

	public virtual bool SkillTargetSelectExcept(Skill ExceptSkill)
	{
		return false;
	}

	public virtual bool CanSkillEnforce(Skill MainSkill)
	{
		return true;
	}

	public virtual string UseSubParticlepath()
	{
		return null;
	}

	public override void Init()
	{
		base.Init();
		if (TargetBuff != null)
		{
			TargetBuff.Clear();
		}
		if (SelfBuff != null)
		{
			SelfBuff.Clear();
		}
	}

	public bool CanSkillEnforceChar(Skill MainSkill)
	{
		if (Data.NeedCharacter == MainSkill.Master.Info.KeyData)
		{
			return false;
		}
		return true;
	}

	public virtual void UseWaste()
	{
	}

	public virtual void ChoiceSkill_Before()
	{
	}

	public virtual void ChoiceSkill_After(Skill ChoicedSkill)
	{
	}

	public virtual void BattleStartDeck(List<Skill> Skills_Deck)
	{
	}

	public virtual void Special_PointerEnter(BattleChar Char)
	{
	}

	public virtual void Special_PointerExit()
	{
	}

	public virtual void Special_SkillButtonPointerEnter()
	{
	}

	public virtual void Special_SkillButtonPointerExit()
	{
	}

	public virtual void Special_SkillButtonPointerClick()
	{
	}

	public virtual void Special_TargetSelectEnable()
	{
	}

	public virtual bool Special_TargetSelectClick(BattleChar Char)
	{
		return true;
	}

	public static Skill_Extended ExtendedClone(BattleChar User, Skill_Extended MainEx, Skill MainSkill)
	{
		Skill_Extended skill_Extended = (Skill_Extended)MainEx.Clone();
		if (User != null)
		{
			skill_Extended.MyChar = User.Info;
		}
		skill_Extended.MySkill = MainSkill;
		if (!string.IsNullOrEmpty(skill_Extended.SkillParticleObject))
		{
			skill_Extended.SkillParticleLive = null;
			skill_Extended.SkillParticleAdd();
			if (MainEx.ParticleOn)
			{
				skill_Extended.ParticleOn = false;
				skill_Extended.SkillParticleOn();
			}
		}
		return skill_Extended;
	}

	public virtual bool TargetHit(BattleChar Target)
	{
		return false;
	}

	public virtual List<BattleChar> Special_Target()
	{
		return null;
	}

	public void SkillBuffAdd(GDEBuffData Buffdata, bool target = true, int tagper = 0)
	{
		BuffTag buffTag = new BuffTag();
		buffTag.BuffData = Buffdata;
		buffTag.PlusTagPer = tagper;
		if (TargetBuff == null)
		{
			TargetBuff = new List<BuffTag>();
		}
		if (SelfBuff == null)
		{
			SelfBuff = new List<BuffTag>();
		}
		if (target)
		{
			TargetBuff.Add(buffTag);
		}
		else
		{
			SelfBuff.Add(buffTag);
		}
	}

	public void SkillBuffAdd(string BuffdataKey, bool target = true, int tagper = 0)
	{
		SkillBuffAdd(new GDEBuffData(BuffdataKey), target, tagper);
	}

	public void SkillParticleAdd()
	{
		if (!(SkillParticleLive == null))
		{
			return;
		}
		if (MySkill.MyButton != null && MySkill.MyButton.SEPos != null)
		{
			SkillParticleLive = Misc.UIInstAddressable(SkillParticleObject, MySkill.MyButton.SEPos, AddressableLoadManager.ManageType.Stage);
			ParticleOn = true;
			SkillParticleOff();
			if (Data != null && Data.AlwaysParticle)
			{
				SkillParticleOn();
			}
			if (MySkill.MyButton.IsUseBig)
			{
				ParticleSystem[] componentsInChildren = SkillParticleLive.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					ParticleSystem.ShapeModule shape = componentsInChildren[i].shape;
					shape.scale = new Vector3(320f, 200f);
				}
			}
		}
		else if (MySkill.BasicSkillButton != null && MySkill.BasicSkillButton.SEPos != null)
		{
			SkillParticleLive = Misc.UIInstAddressable(SkillParticleObject, MySkill.BasicSkillButton.SEPos, AddressableLoadManager.ManageType.Stage);
			ParticleSystem[] componentsInChildren = SkillParticleLive.GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem particleSystem in componentsInChildren)
			{
				ParticleSystem.MinMaxCurve rateOverTime = particleSystem.emission.rateOverTime;
				rateOverTime.constant = particleSystem.emission.rateOverTime.constant * 0.66f;
				ParticleSystem.EmissionModule emission = particleSystem.emission;
				emission.rateOverTime = rateOverTime;
				ParticleSystem.ShapeModule shape2 = particleSystem.shape;
				shape2.scale = new Vector3(110f, 90f);
			}
			ParticleOn = true;
			SkillParticleOff();
			if (Data != null && Data.AlwaysParticle)
			{
				SkillParticleOn();
			}
		}
	}

	public void SkillParticleOn()
	{
		if (!ParticleOn && SkillParticleLive != null)
		{
			ParticleSystem[] componentsInChildren = SkillParticleLive.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.EmissionModule emission = componentsInChildren[i].emission;
				emission.enabled = true;
			}
			if (SkillParticleLive.GetComponent<ParticleSystem>() != null)
			{
				ParticleSystem.EmissionModule emission2 = SkillParticleLive.GetComponent<ParticleSystem>().emission;
				emission2.enabled = true;
			}
			ParticleOn = true;
		}
	}

	public void SkillParticleOff()
	{
		if (ParticleOn && SkillParticleLive != null)
		{
			ParticleSystem[] componentsInChildren = SkillParticleLive.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.EmissionModule emission = componentsInChildren[i].emission;
				emission.enabled = false;
			}
			if (SkillParticleLive.GetComponent<ParticleSystem>() != null)
			{
				ParticleSystem.EmissionModule emission2 = SkillParticleLive.GetComponent<ParticleSystem>().emission;
				emission2.enabled = false;
			}
			ParticleOn = false;
		}
	}

	public static Skill_Extended DataToExtended(GDESkillExtendedData _Data)
	{
		Skill_Extended skill_Extended = new Skill_Extended();
		Type type = ModManager.GetType(_Data.ClassName);
		if (type != null)
		{
			skill_Extended = (Skill_Extended)Activator.CreateInstance(type);
		}
		else
		{
			skill_Extended.fail = true;
			Debug.LogError("Extended's ClassName not found: " + _Data.Key);
		}
		skill_Extended.Data = _Data;
		skill_Extended._Des = _Data.Des;
		if (!string.IsNullOrEmpty(_Data.Image_Path))
		{
			skill_Extended.SpriteImage = AddressableLoadManager.LoadAsyncCompletion<Sprite>(_Data.Image_Path, AddressableLoadManager.ManageType.Stage);
		}
		return skill_Extended;
	}

	public static Skill_Extended DataToExtended(string Key)
	{
		return DataToExtended(new GDESkillExtendedData(Key));
	}

	public static Skill_Extended DataToExtendedC(string ClassKey)
	{
		Skill_Extended skill_Extended = new Skill_Extended();
		Type type = ModManager.GetType(ClassKey);
		if (type != null)
		{
			skill_Extended = (Skill_Extended)Activator.CreateInstance(type);
		}
		else
		{
			skill_Extended.fail = true;
			Debug.LogError("스킬 익스텐디드 클래스 이름 찾을 수 없음 : " + ClassKey);
		}
		return skill_Extended;
	}

	public Skill_Extended Set()
	{
		return this;
	}

	public virtual void HandInit()
	{
	}

	public virtual void AttackEffectSingle(BattleChar hit, SkillParticle SP, int DMG, int Heal)
	{
	}

	public virtual void BeforeHeal(BattleChar hit, SkillParticle SP, float Heal, bool Cri)
	{
	}

	public virtual void SkillTargetSingle(List<Skill> Targets)
	{
	}

	public virtual void EnemyCastEnqueueInit()
	{
	}

	public virtual void SkillUseSingle(Skill SkillD, List<BattleChar> Targets)
	{
	}

	public virtual void SkillUseSingleAfter(Skill SkillD, List<BattleChar> Targets)
	{
	}

	public virtual void SkillUseHandBefore()
	{
	}

	public virtual void SkillUseHand(BattleChar Target)
	{
	}

	public virtual bool FakeAct()
	{
		return false;
	}

	public virtual void SkillUseHandIsFreeUseing()
	{
	}

	public virtual void TickUpdate()
	{
	}

	public virtual bool TargetSelectExcept(BattleChar ExceptTarget)
	{
		return false;
	}

	public virtual IEnumerator SkillUseSingle_IEnum(Skill SkillD, List<BattleChar> Targets)
	{
		yield return null;
	}

	public virtual IEnumerator DrawAction()
	{
		yield return null;
	}

	public virtual bool Terms()
	{
		return DefuiltTerms;
	}

	public virtual bool ButtonSelectTerms()
	{
		return DefuiltTerms;
	}

	public virtual void ButtonInit()
	{
	}

	public virtual void DiscardSingle(bool Click)
	{
	}

	public new virtual string DescExtended(string desc)
	{
		return desc;
	}

	public virtual void SkillKill(SkillParticle SP)
	{
	}

	public virtual void SkillKillDummy(SkillParticle SP)
	{
	}

	public virtual void SelfDestroy()
	{
		if (!isDestroy)
		{
			isDestroy = true;
			if (BuffIcon != null)
			{
				UnityEngine.Object.Destroy(BuffIcon);
			}
			for (int i = 0; i < MySkill.AllExtendeds.Count; i++)
			{
				SkillParticleOff();
			}
		}
	}

	public virtual object Clone()
	{
		Skill_Extended skill_Extended = MemberwiseClone() as Skill_Extended;
		skill_Extended.TargetBuff = new List<BuffTag>();
		skill_Extended.SelfBuff = new List<BuffTag>();
		skill_Extended.TargetBuff.Clear();
		skill_Extended.BuffIcon = null;
		foreach (BuffTag item in TargetBuff)
		{
			skill_Extended.TargetBuff.Add(item.Clone());
		}
		skill_Extended.SelfBuff.Clear();
		foreach (BuffTag item2 in SelfBuff)
		{
			skill_Extended.SelfBuff.Add(item2.Clone());
		}
		return skill_Extended;
	}

	public virtual void WhenAnotherClone_BattleExOnly(Skill_Extended NewExended)
	{
	}

	public virtual void UsedDeckInit()
	{
	}

	public bool CanEnforce(Skill EnforceSkill)
	{
		if (!EnforceSkill.Enforce && !EnforceSkill.Enforce_CantUse && !EnforceSkill.Enforce_Weak && EnforceSkill.MySkill.Category.Key != GDEItemKeys.SkillCategory_DefultSkill && CanSkillEnforce(EnforceSkill) && CanSkillEnforceChar(EnforceSkill))
		{
			return true;
		}
		return false;
	}
}
