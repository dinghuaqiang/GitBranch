using PureMVC.Patterns;
using System.Collections;
using System.Collections.Generic;
using TableTool;
using UnityEngine;

public class AbyssBuffMgr
{
    public const int BUFF_TARGET_ALL = 1;
    public const int BUFF_TARGET_LEADER = 2;
    public const int BUFF_TARGET_ROLE = 2;

    public const int BUFF_EFFECT_ALL = 1;
    public const int BUFF_EFFECT_LAYER = 2;

    public const int BUFF_RED = 1;
    public const int BUFF_BULE = 2;

    public System.Action<int> OnBuffPointChange = null;
    public System.Action<int, int> OnBuffPointShowChange = null;
    public System.Action<FlyStruct, int> OnGetFlyData = null;

    int[] mBuffPoints;
    List<BuffInfo> mLearnBuff;
    TeamMgr mTeamMgr;

    public int BDSkill2Blue;
    public int ZDSkill2Red;
    public int EVOSkill2Red;


    List<BuffInfo> __tmp_list_buffinfo = new List<BuffInfo>();
    public void InitOnce()
    {
        mTeamMgr = TeamMgr.Ins;
        BDSkill2Blue = Config_NewconfigModel.Ins.GetValue_Int(700002, 0);
        //ZDSkill2Red = Config_NewconfigModel.Ins.GetValue_Int(700001, 0);
        EVOSkill2Red = Config_NewconfigModel.Ins.GetValue_Int(700003, 0);
    }

    public void Init()
    {
        mBuffPoints = new int[3];
        mLearnBuff = new List<BuffInfo>();
    }
    public void DeInit()
    {
        mBuffPoints = null;
        mLearnBuff.Clear();
        _RandomCanUseHero_records.Clear();
    }


    public List<BuffInfo> GetLearnBuff()
    {
        return mLearnBuff;
    }

    public BuffInfo GetBuff_Id(int id)
    {
        for (int i = 0; i < mLearnBuff.Count; i++)
        {
            var buff = mLearnBuff[i];
            if (buff.id == id) return buff;
        }
        return null;
    }
    public List<int> RandomBuffs()
    {
        List<int> re = new List<int>();
        RandomBuffs(3,-1, re);
        return re;
    }

    List<int> __RandomBuffs_weightarr = new List<int>();
    List<int> __RandomBuffs_choosids = new List<int>();
    void RandomBuffs(int count,int type, List<int> re)
    {
        var allitems = Buff_redAndBlueModle.Ins.GetAllBeans();

        int allweight = 0;

        __RandomBuffs_weightarr.Clear();
        __RandomBuffs_choosids.Clear();
        List<int> weightarr = __RandomBuffs_weightarr;
        List<int> choosids = __RandomBuffs_choosids;
        int curfloor = GameLogic.Release.Mode.ROTmodeFloor;
        bool canranodmhero = RandomCanUseHero(false) > 0 && TeamMgr.Ins.mTeamsCount < 2; //大秘境限制队友数量最多两个
        List<int> record_ee = new List<int>();
        for (int i = 0; i < allitems.Count; i++)
        {
            var item = allitems[i];

            if (item.Weights <= 0) continue;
            //if (item.AvailableLocation > curfloor) continue;
            if (type >= 0 && type != item.Type) continue;
            if (item.IsEvoBuff()) continue;
            int evoid = Buff_redAndBlueModle.Ins.GetEvoBuffId_Id(item.BuffID);
            if (evoid > 0 && ContainBuff(evoid)) continue;
            if (item.EffectEnumeration > 1)
            {
                if (record_ee.Contains(item.EffectEnumeration)) continue;
                record_ee.Add(item.EffectEnumeration);
            }

            if (item.Repeatable == 0)
            {
                if (ContainBuff(item.BuffID))
                {
                    continue;
                }
            }

            if (item.EffectEnumeration == 2)
            {
                if (!canranodmhero)
                {
                    continue;
                }
            }

            allweight += item.Weights;
            weightarr.Add(item.Weights);
            choosids.Add(item.BuffID);
        }

        for (int i = 0; i < count; i++)
        {
            int randomv = UnityEngine.Random.Range(1, allweight + 1);
            for (int j = 0; j < weightarr.Count; j++)
            {
                int w = weightarr[j];
                randomv -= w;
                if (randomv <= 0)
                {
                    re.Add(choosids[j]);
                    allweight -= w;
                    weightarr[j] = 0;
                    break;
                }
            }
        }
    }
    bool ContainBuff(int id)
    {
        for (int i = 0; i < mLearnBuff.Count; i++)
        {
            if (mLearnBuff[i].id == id) return true;
        }
        return false;
    }

    public void ChangePoint_Buffid(int id)
    {
        Buff_redAndBlue cfgitem = Buff_redAndBlueModle.Ins.GetBeanById(id);
        if (cfgitem == null)
        {
            Debug.LogError($"ChangePoint_Buffid no cfg:{id}");
            return;
        }
        ChangePoint(cfgitem.Type, -cfgitem.price);
    }
    public void ChangePoint(int type, int v)
    {
        if (v == 0) return;

        int p = mBuffPoints[type];
        p += v;
        mBuffPoints[type] = p;
        if (p < 0)
        {
            Debug.LogError($"ChangePoint < 0 :{type}={v}");
        }
        if (OnBuffPointChange != null)
        {
            OnBuffPointChange(type);
        }
        PointChangeShow(type, p);
    }

    public class FlyStruct
    {
        public Sprite icon;
        public Vector3 startpos;
        public Vector3 endpos;
    }
    public void ShowFly(int type, int v)
    {
        if (v <= 0) return;

        if (OnGetFlyData == null) return;

        int cur = GetPoint(type);
        int old = cur - v;
        FlyStruct data = new FlyStruct();
        OnGetFlyData(data, type);
        CurrencyFlyCtrl.PlayGet_Tmp(data.icon, old, cur, data.startpos, data.endpos, (l) =>
        {
            TeamMgr.Ins.BuffMgr.PointChangeShow(type, (int)l);
        });
    }

    public void PointChangeShow(int type, int v)
    {
        if (type == BUFF_RED)
        {
            Facade.Instance.SendNotification("ROT_RED_BUFF_CHANGE", v);
        }
        else if (type == BUFF_BULE)
        {
            Facade.Instance.SendNotification("ROT_BLUE_BUFF_CHANGE", v);
        }
        if (OnBuffPointShowChange != null)
        {
            OnBuffPointShowChange(type, v);
        }
    }

    public int GetPoint(int type)
    {
        return mBuffPoints[type];
    }
    public void LearnBuff(int id,params object[] data)
    {
        if (CheckLearnEvoBuff(id))
        {
            return;
        }
        var info = CreatBuffInfo(id);
        info.data = data;

        if (info.Cfg.EffectEnumeration != 1)
        {
            HandleBuff(info, null);
            return;
        }
        var targets = GetEffectHero(info.Cfg);
        for (int i = 0; i < targets.Count; i++)
        {
            var hero = targets[i];
            HandleBuff(info, hero);
        }
    }
    bool CheckLearnEvoBuff(int id)
    {
        if (Buff_redAndBlueModle.Ins.IsEvoBuff(id))
        {
            return false;
        }
        int evoid = Buff_redAndBlueModle.Ins.GetEvoBuffId_Id(id);
        if (evoid == 0)
        {
            return false;
        }
        var evocfg = Buff_redAndBlueModle.Ins.GetBeanById(evoid);
        var connectids = evocfg.Connect;
        __tmp_list_buffinfo.Clear();
        bool allhave = true;
        for (int i = 0; i < connectids.Length; i++)
        {
            int cid = connectids[i];
            if (cid == id)
            {
                continue;
            }
            var cbuff = GetBuff_Id(cid);
            if (cbuff == null)
            {
                allhave = false;
                break;
            }
            __tmp_list_buffinfo.Add(cbuff);
        }
        if (!allhave)
        {
            return false;
        }
        for (int i = 0; i < __tmp_list_buffinfo.Count; i++)
        {
            var info = __tmp_list_buffinfo[i];
            RemoveBuff(info);
        }
        LearnBuff(evoid);
        return true;
    }
    public void HandleHero(EntityHero hero)
    {
        GameLogic.Swith_MaxChangeDisplay = false;
        for (int i = 0; i < mLearnBuff.Count; i++)
        {
            var info = mLearnBuff[i];
            if (info.Cfg.EffectEnumeration != 1) continue;
            bool effect = IsEffectTarget(hero.ClassID, info);
            if (effect)
            {
                HandleBuff(info, hero);
            }
        }
        GameLogic.Swith_MaxChangeDisplay = true;
    }
    public void OnToNextLayer()
    {
        for (int i = mLearnBuff.Count - 1; i >= 0; i--)
        {
            var info = mLearnBuff[i];
            if (info.Cfg.EffectType == BUFF_EFFECT_LAYER)
            {
                info.effectvalue++;
                if (CheckBuffEnd_2(info))
                {
                    RemoveBuff(info);
                }
            }
        }
    }
    bool IsEffectTarget(int heroid, BuffInfo info)
    {
        var cfg = info.Cfg;
        if (info.owner == 0)
        {
            int effectunit = cfg.EffectiveUnit[0];

            if (effectunit == BUFF_TARGET_ALL)
            {
                return true;
            }
            else if (effectunit == BUFF_TARGET_ROLE)
            {
                for (int j = 1; j < cfg.EffectiveUnit.Length; j++)
                {
                    if (cfg.EffectiveUnit[j] == heroid)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            if (info.owner == heroid)
            {
                return true;
            }
        }
        return false;
    }

    List<int> _RandomCanUseHero_records = new List<int>();
    public int RandomCanUseHero(bool record = true)
    {
        Dictionary<string, LocalSave.HeroOne> heroDic = LocalSave.Instance.mHero.GetAllUnlockHero();

        List<int> list_id = new List<int>();
        foreach (var item in heroDic)
        {
            int hid = int.Parse(item.Value.HeroID);
            if (_RandomCanUseHero_records.Contains(hid)) continue;
            if (mTeamMgr.IsHeroUsed(hid)) continue;
            list_id.Add(hid);
        }
        if (list_id.Count == 0)
        {
            return 0;
        }
        int reid = list_id[Random.Range(0, list_id.Count)];

        if (record)
        {
            _RandomCanUseHero_records.Add(reid);
        }

        return reid;
    }

    #region HandleBuff
    void HandleBuff(BuffInfo info, EntityHero target)
    {
        if (info.Cfg.EffectEnumeration == 1)
        {
            HandleBuff_1(info, target);
        }
        else if (info.Cfg.EffectEnumeration == 2)
        {
            HandleBuff_2(info, target);
        }
        else if (info.Cfg.EffectEnumeration == 3)
        {
            HandleBuff_3(info, target);
        }
    }

    void HandleBuff_1(BuffInfo info, EntityHero target)
    {
        int num = info.Cfg.Value.Length;
        if (num > 0)
        {
            target.m_EntityData.m_Switch_MaxHP2CurHP = false;
            for (int i = 0; i < num; i++)
            {
                Goods_goods.GoodData goodData = Goods_goods.GetGoodData(info.Cfg.Value[i]);
                target.m_EntityData.ExcuteAttributes(goodData);
            }
            target.m_EntityData.m_Switch_MaxHP2CurHP = true;
        }
    }
    void HandleBuff_2(BuffInfo info, EntityHero target)
    {      
        int heroid = 0;
        if (info.data != null && info.data.Length > 0)
        {
            heroid = (int)info.data[0];
        }
        else
        {
            heroid = RandomCanUseHero();
        }
        if (heroid <= 0) return;

        mTeamMgr.AddTeamHero(heroid);
    }
    void HandleBuff_3(BuffInfo info, EntityHero target)
    {
        var value = info.Cfg.Value;
        int add_zd = int.Parse(value[0]);
        int add_bd = int.Parse(value[1]);

        mTeamMgr.Skill_Count_Add_ZD += add_zd;
        mTeamMgr.Skill_Count_Add_BD += add_bd;

        if (mTeamMgr.OnSkillCount_AddChange != null)
        {
            mTeamMgr.OnSkillCount_AddChange();
        }
    }

    List<EntityHero> _RemoveBuff_1_heros = new List<EntityHero>();

    void RemoveBuff(BuffInfo info)
    {
        if (info.Cfg.EffectEnumeration == 1)
        {
            RemoveBuff_1(info);
        }
        else if (info.Cfg.EffectEnumeration == 2)
        {
            RemoveBuff_2(info);
        }
        else if (info.Cfg.EffectEnumeration == 3)
        {
            RemoveBuff_3(info);
        }
    }
    void RemoveBuff_1(BuffInfo info)
    {
        mLearnBuff.Remove(info);
        var cfg = info.Cfg;
        _RemoveBuff_1_heros.Clear();
        TeamMgr.Ins.GetAllHero(_RemoveBuff_1_heros);
        for (int i = 0; i < _RemoveBuff_1_heros.Count; i++)
        {
            var hero = _RemoveBuff_1_heros[i];
            if (IsEffectTarget(hero.ClassID, info))
            {
                int num = cfg.Value.Length;
                if (num > 0)
                {
                    for (int j = 0; j < num; j++)
                    {
                        Goods_goods.GoodData goodData = Goods_goods.GetGoodData(cfg.Value[j]);
                        goodData.value = -goodData.value;
                        hero.m_EntityData.ExcuteAttributes(goodData);
                    }
                }
            }
        }
    }
    void RemoveBuff_2(BuffInfo info)
    {
        mLearnBuff.Remove(info);
    }
    void RemoveBuff_3(BuffInfo info)
    {
        mLearnBuff.Remove(info);
        var value = info.Cfg.Value;
        int add_zd = int.Parse(value[0]);
        int add_bd = int.Parse(value[1]);
        mTeamMgr.Skill_Count_Add_ZD -= add_zd;
        mTeamMgr.Skill_Count_Add_BD -= add_bd;
        if (mTeamMgr.OnSkillCount_AddChange != null)
        {
            mTeamMgr.OnSkillCount_AddChange();
        }
    }
    #endregion
    #region CheckBuffEnd
    bool CheckBuffEnd_1(BuffInfo info)
    {
        return false;
    }
    bool CheckBuffEnd_2(BuffInfo info)
    {
        return info.effectvalue > info.Cfg.EffectTypeValue;
    }
    #endregion
    BuffInfo CreatBuffInfo(int id)
    {
        BuffInfo info = new BuffInfo();
        info.id = id;
        int effectunit = info.Cfg.EffectiveUnit[0];
        if (effectunit == BUFF_TARGET_LEADER)
        {
            info.owner = mTeamMgr.MainHero.ClassID;
        }
        if (info.Cfg.EffectType == BUFF_EFFECT_LAYER)
        {
            info.effectvalue = 1;
        }
        mLearnBuff.Add(info);
        return info;
    }
    List<EntityHero> GetEffectHero(Buff_redAndBlue cfg)
    {
        List<EntityHero> re = new List<EntityHero>();
        int effectunit = cfg.EffectiveUnit[0];
        if (effectunit == BUFF_TARGET_LEADER)
        {
            re.Add(mTeamMgr.MainHero);
        }
        else if (effectunit == BUFF_TARGET_ALL)
        {
            mTeamMgr.GetAllHero(re);
        }
        else if (effectunit == BUFF_TARGET_ROLE)
        {
            for (int i = 1; i < cfg.EffectiveUnit.Length; i++)
            {
                var hero = mTeamMgr.GetHero_Id(cfg.EffectiveUnit[i]);
                if (hero != null)
                {
                    re.Add(hero);
                }
            }
        }
        return re;
    }
    public class BuffInfo
    {
        public int id;
        public int owner;//0=配置  1011=角色
        public int effectvalue;//已生效数据,不同类型,不同含义
        public object[] data;
        public void SetData(SaveData_TeamData.BuffInfo info)
        {
            id = info.id;
            owner = info.owner;
            effectvalue = info.effectvalue;
        }

        Buff_redAndBlue _cfg = null;
        public Buff_redAndBlue Cfg
        {
            get
            {
                if (_cfg == null)
                {
                    _cfg = Buff_redAndBlueModle.Ins.GetBeanById(id);
                    if (_cfg == null)
                    {
                        Debug.LogError($"BuffInfo no cfg:{id}");
                    }
                }
                return _cfg;
            }
        }
    }

    #region LocalSave
    public void LocalSave_Load(SaveData_TeamData.TeamData data)
    {
        mBuffPoints[BUFF_BULE] = data.buffBule;
        mBuffPoints[BUFF_RED] = data.buffRed;

        mLearnBuff.Clear();
        for (int i = 0; i < data.teamBuffs.Count; i++)
        {
            var tmp = data.teamBuffs[i];
            if (Buff_redAndBlueModle.Ins.GetBeanById(tmp.id) == null) continue;
            BuffInfo info = new BuffInfo();
            info.SetData(tmp);
            mLearnBuff.Add(info);
        }
    }
    public void LocalSave_Save(SaveData_TeamData.TeamData data)
    {
        data.buffBule = mBuffPoints[BUFF_BULE];
        data.buffRed = mBuffPoints[BUFF_RED];

        data.teamBuffs.Clear();

        for (int i = 0; i < mLearnBuff.Count; i++)
        {
            var tmp = mLearnBuff[i];
            SaveData_TeamData.BuffInfo info = new SaveData_TeamData.BuffInfo();
            info.SetData(tmp);
            data.teamBuffs.Add(info);
        }
    }
    #endregion
}