using Newtonsoft.Json;
using Pbmsg;
using PureMVC.Patterns;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TableTool;
using UnityEngine;

public class TeamMgr
{
    #region Static
    static TeamMgr _ins = null;

    public static TeamMgr Ins
    {
        get
        {
            if (_ins == null)
            {
                _ins = new TeamMgr();
                _ins.InitOnce();
            }
            return _ins;
        }
    }
    #endregion

    public bool IsOpen = false;

    public List<int> TeamRolesTmp = new List<int>();

    public System.Action OnTeamChange = null;

    public float ChangeHero_Timer;

    public bool Deinited = true;

    public bool IsPlay = false;

    EntityHero mSelf;
    public int mTeamsCount => mTeams.Count;
    List<AbyssHeroAI> mTeams;
    List<int> mTeamskill;
    List<HeroInfo> mHeroInfos;
    HeroInfo mSelfHeroInfo;

    List<int> mMemberRecord;

    bool[] mPosState;
    int[] mPosUnlockCost;

    float P_Time2RedBuff;

    AbyssBuffMgr mBuffMgr;

    bool mCanChangePlayer = false;

    public System.Action OnSkillCount_AddChange = null;

    public int Skill_Count_Add_ZD;
    public int Skill_Count_Add_BD;

    public bool CanChangePlayer
    {
        get
        {
            if (!IsOpen) return false;
            return mCanChangePlayer;
        }
    }

    public EntityHero MainHero
    {
        get
        {
            return mSelf;
        }
    }

    public AbyssBuffMgr BuffMgr
    {
        get
        {
            return mBuffMgr;
        }
    }

    public void InitOnce()
    {
        mBuffMgr = new AbyssBuffMgr();
        mBuffMgr.InitOnce();
        mTeams = new List<AbyssHeroAI>();
        mTeamskill = new List<int>();
        mHeroInfos = new List<HeroInfo>();
        mMemberRecord = new List<int>();
        mPosState = new bool[2];
        mPosUnlockCost = new int[2];
        mPosUnlockCost[0] = Config_NewconfigModel.Ins.GetValue_Int(700005, 0);
        mPosUnlockCost[1] = Config_NewconfigModel.Ins.GetValue_Int(700006, 0);
        P_Time2RedBuff = Config_NewconfigModel.Ins.GetValue_Float(700001, 0);
    }


    public void Init()
    {
        Skill_Count_Add_ZD = 0;
        Skill_Count_Add_BD = 0;
        Deinited = true;
        mBuffMgr.Init();
        ChangeHero_Timer = 0;
        IsPlay = false;
    }

    public void SetPlay()
    {
        IsPlay = true;
    }

    public void OnCreatSelf()
    {
        if (TeamRolesTmp.Count == 0)
        {
            int id;
            if (!int.TryParse(LocalSave.Instance.mHero.GetUseHero().HeroID, out id))
            {
                id = 1011;
            }
            if (!LocalSave.Instance.GetIsGuid())
            {
                id = 1011;
            }
            TeamRolesTmp.Add(id);
        }
        CreatSelf(TeamRolesTmp[0], Vector3.zero, true);
    }

    public void DeInit()
    {
        IsPlay = false;
        DeInit_Level(true);
        mBuffMgr.DeInit();
        mTeams.Clear();
        mTeamskill.Clear();
        mHeroInfos.Clear();
        mMemberRecord.Clear();
        TeamRolesTmp.Clear();
        for (int i = 0; i < mPosState.Length; i++)
        {
            mPosState[i] = false;
        }
    }

    public void Init_Level(bool isinit = false)
    {
        Deinited = false;
        InitTeam(TeamRolesTmp, isinit);
        GamePlay();
        if (!isinit)
        {
            Facade.Instance.SendNotification("GET_SKILL_ONMAP");
        }      
    }

    public void DeInit_Level(bool isover = false)
    {
        Deinited = true;
        if (!isover)
        {
            TeamRolesTmp.Clear();
            TeamRolesTmp.Add(mSelf.ClassID);
            for (int i = 0; i < mTeams.Count; i++)
            {
                TeamRolesTmp.Add(mTeams[i].ClassID);
            }
            mMemberRecord.Clear();
        }
        if (mTeams != null)
        {
            for (int i = 0; i < mTeams.Count; i++)
            {
                mTeams[i].TeamDeInit();
            }
            mTeams.Clear();
        }
        if (!isover)
        {
            mSelf.TeamDeInit();
            mSelf = null;
        }       
    }

    //0=小门 1=大门
    public void OnEnterDoor(int type)
    {
        DeInit_Level();
    }

    //0=小门 1=大门
    public void OnOutDoor(int type)
    {
        Init_Level();        
    }

    public int TeamSkills2BlueBuff_Value()
    {
        if (mTeamskill.Count == 0) return 0;

        int point = 0;
        for (int i = 0; i < mTeamskill.Count; i++)
        {
            var id = mTeamskill[i];
            var skillinfo = SkillMgr.Ins.GetSkillInfo_SkillId(id);
            point += (mBuffMgr.BDSkill2Blue * (skillinfo.GetLevel(id) + 1));
        }      
        return point;
    }

    public int TeamSkills2BlueBuff(bool showfly = true)
    {
        int point = TeamSkills2BlueBuff_Value();
        if (point == 0) return point;
        mBuffMgr.ChangePoint(AbyssBuffMgr.BUFF_BULE, point);
        mTeamskill.Clear();
        if (showfly)
        {
            mBuffMgr.ShowFly(AbyssBuffMgr.BUFF_BULE, point);
        }
        return point;
    }

    public int Cal_RefBuff(int time)
    {
        return Mathf.CeilToInt(P_Time2RedBuff * time);
    }
    public void GameTime2RedBuff(int time,bool showfly = true)
    {
        int v_add = Cal_RefBuff(time);
        mBuffMgr.ChangePoint(AbyssBuffMgr.BUFF_RED, v_add);
        if (showfly)
        {
            mBuffMgr.ShowFly(AbyssBuffMgr.BUFF_RED, v_add);
        }
    }
    public void GamePlay()
    {
        for (int i = 0; i < mTeams.Count; i++)
        {
            mTeams[i].StartGame();
        }
    }

    bool HandleCreatHero_Skill(EntityHero hero)
    {
        bool re = false;
        var info = GetHeroInfo(hero.ClassID);
        re = info.skills.Count>0;
        for (int i = 0; i < info.skills.Count; i++)
        {
            hero.LearnSkill(info.skills[i]);
        }
        for (int i = 0; i < mTeamskill.Count; i++)
        {
            hero.LearnSkill(mTeamskill[i]);
        }
        return re;
    }

    EntityHero CreatSelf(int id, Vector3 pos, bool nopos = false)
    {
        Debug.Log($"TeamMgr.CreateSelf -----init hero.");
        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ResourceManager.Load<GameObject>("Game/Player/PlayerNode"));
        gameObject.transform.parent = GameNode.m_Battle.transform;
        LocalSave.HeroOne heroone = LocalSave.Instance.mHero.GetHeroByHeroID(id.ToString());
        GameLogic.SelfAttribute.Init(heroone);
        EntityHero component = gameObject.GetComponent<EntityHero>();
        component.SetSelf();
        GameLogic.SetSelf(component);
        component.Init(id, true, null, LocalSave.Instance.GetCharacterSkinIdx(id));
        mSelf = component;
        mBuffMgr.HandleHero(component);
        mSelfHeroInfo = GetHeroInfo(id);
        bool havemainskill = HandleCreatHero_Skill(component);
        SkillMgr.Ins.OnChangeHero(component.GetSkillList(), havemainskill);
        var info = GetHeroInfo(id);
        component.m_EntityData.SetCurrentExpLevel(info.exp, info.level);
        component.m_EntityData.OnExpChange += OnOnExpChange;
        component.OnChangeHPAction += OnChangeHPAction;
        if (info.hp > 0)
        {
            component.m_EntityData.SetCurrentHP(info.hp);
        }
        if (!nopos)
        {
            component.SetPosition(pos);
            EntityMoveCtrl.Instance?.Add(component);
        }
        return component;
    }

    private void OnChangeHPAction(long hp, long arg2, float arg3, long arg4)
    {
        mSelfHeroInfo.hp = hp;
    }

    private void OnOnExpChange()
    {
        mSelfHeroInfo.level = mSelf.m_EntityData.GetLevel();
        mSelfHeroInfo.exp = (int)mSelf.m_EntityData.GetCurrentExp();
    }
    AbyssHeroAI CreatHeroAI(int id, Vector3 pos, bool nopos = false)
    {
        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ResourceManager.Load<GameObject>("Game/Player/AbyssHeroAINode"));
        gameObject.transform.parent = GameNode.m_Battle.transform;
        LocalSave.HeroOne heroone  = LocalSave.Instance.mHero.GetHeroByHeroID(id.ToString());
        GameLogic.HeroAttribute.Init(heroone);
        AbyssHeroAI component = gameObject.GetComponent<AbyssHeroAI>();
        component.Init(id, true, null, LocalSave.Instance.GetCharacterSkinIdx(id));
        mTeams.Add(component);
        mBuffMgr.HandleHero(component);
        if (!HandleCreatHero_Skill(component))
        {
            SkillMgr.Ins.LearnMainSkill(component);
        }
        if (!nopos)
        {
            component.SetPosition(pos);
            EntityMoveCtrl.Instance?.Add(component);
        }
        var info = GetHeroInfo(id);
        component.m_EntityData.SetCurrentExpLevel(info.exp, info.level);
        return component;
    }
    public void InitTeam(List<int> ids, bool isinit = false)
    {
        Vector3 pos = Vector3.zero;
        bool nopos = true;
        if (GameLogic.Release.Mode.RoomGenerate !=null)
        {
            pos = GameLogic.Release.Mode.RoomGenerate.PlayerPos;
            nopos = false;
        }

        if (!isinit)
        {
            CreatSelf(ids[0], pos, nopos);
        }
        for (int i = 1; i < ids.Count; i++)
        {
            CreatHeroAI(ids[i], pos, nopos);
        }
        SelfPlayerChange();
        RefreshTeamQueue();
    }

    HeroInfo AddHeroInfo(int id)
    {
        HeroInfo info = new HeroInfo();
        info.id = id;
        info.state = 1;
        mHeroInfos.Add(info);
        return info;
    }

    HeroInfo GetHeroInfo(int id, bool null2add = true)
    {
        HeroInfo re = null;
        for (int i = 0; i < mHeroInfos.Count; i++)
        {
            var info = mHeroInfos[i];
            if (info.id == id)
            {
                re = info;
                break;
            }
        }
        if (re == null && null2add)
        {
            re = AddHeroInfo(id);
        }
        if (re != null)
        {
            if (re.skills.Count == 0)
            {
                re.skills.AddRange(SkillMgr.Ins.GetMainSkill_SkillId(id));
            }
        }
        return re;
    }
    public List<int> GetHeroSkills(int id)
    {
        for (int i = 0; i < mHeroInfos.Count; i++)
        {
            var info = mHeroInfos[i];
            if (info.id == id) return info.skills;
        }
        return null;
    }

    public int GetHeroState(int id)
    {
        for (int i = 0; i < mHeroInfos.Count; i++)
        {
            var info = mHeroInfos[i];
            if (info.id == id) return info.state;
        }
        return 1;
    }
    public void SetHeroState(int id, int state)
    {
        var heroinfo = GetHeroInfo(id);
        heroinfo.state = state;
    }
    public bool IsHeroUsed(int id)
    {
        if (Deinited)
        {
            for (int i = 0; i < TeamRolesTmp.Count; i++)
            {
                if (TeamRolesTmp[i] == id) return true;
            }
            return false;
        }
        if (mSelf.ClassID == id) return true;
        for (int i = 0; i < mTeams.Count; i++)
        {
            var re = mTeams[i];
            if (re.ClassID == id) return true;
        }
        return false;
    }
    public EntityHero GetHero_Id(int id)
    {
        if (mSelf.ClassID == id) return mSelf;

        for (int i = 0; i < mTeams.Count; i++)
        {
            var re = mTeams[i];
            if (re.ClassID == id) return re;
        }
        return null;
    }

    public int GetHeroId_Index(int index)
    {
        if (Deinited)
        {
            if (TeamRolesTmp.Count <= index) return -1;
            return TeamRolesTmp[index];
        }
        if (index == 0)
        {
            if (mSelf.RealDead) return 0;
            return mSelf.ClassID;
        }
        if (mTeams.Count <= index - 1) return -1;
        return mTeams[index - 1].ClassID;
    }

    public void GetAllHero(List<EntityHero> all)
    {
        if (mSelf != null)
        {
            all.Add(mSelf);
        }
        if (mTeams != null && mTeams.Count > 0)
        {
            all.AddRange(mTeams);
        }
    }

    public void RefreshCanChangePlayerState()
    {
        if (!IsOpen) return;
        mCanChangePlayer = mTeams.Count > 0;
    }

    public bool HandlePlayerDead()
    {
        if (mTeams.Count == 0)
        {
            SetHeroState(mSelf.ClassID, 2);
            return false;
        }
        SetHeroState(mSelf.ClassID, 2);
        int id = mTeams[0].ClassID;
        AbyssHeroAI heroai = GetHeroAI(id);
        if (heroai != null)
        {
            mSelf.TeamDeInit();
            Vector3 slefpos = mSelf.position;
            EntityHero hero = CreatSelf(heroai.ClassID, slefpos);

            heroai.TeamDeInit();
            mTeams.Remove(heroai);

            SelfPlayerChange();
            RefreshTeamQueue();
        }
        return true;
    }

    public bool CanChangeHero()
    {
        if (mTeams == null)
        {
            return TeamRolesTmp.Count > 1;
        }
        return mTeams.Count > 0;
    }
    public bool ChangeHero()
    {
        if (mTeams.Count == 0) return false;

        var heroai = mTeams[0];
        Vector3 slefpos = mSelf.position;
        int slefid = mSelf.ClassID;

        mSelf.TeamDeInit();

        EntityHero hero = CreatSelf(heroai.ClassID, slefpos);   

        heroai.TeamDeInit();
        mTeams.RemoveAt(0);

        AbyssHeroAI newheroai = CreatHeroAI(slefid, mSelf.position);

        SelfPlayerChange();
        RefreshTeamQueue();

        newheroai.StartGame();

        Facade.Instance.SendNotification("GET_SKILL_ONMAP");

        return true;
    }

    public void AddTeamHero(int id)
    {
        AbyssHeroAI newheroai = CreatHeroAI(id, mSelf.position);
        SelfPlayerChange();
        RefreshTeamQueue();
        newheroai.StartGame();
        Facade.Instance.SendNotification("GET_SKILL_ONMAP");
    }

    public void OnLearnSkill_After(SkillMgr.SkillInfo skillinfo)
    {
        if (skillinfo.type == SkillMgr.SkillType.ZhuDong)
        {
            RefreshSelfHeroInfo_Skill();
        }   
        else if (skillinfo.type == SkillMgr.SkillType.BeiDong)
        {
            if (skillinfo.curlevel > 0)
            {
                RemoveSkill_Team(skillinfo.GetLastSkill());
            }
            LearnSkill_Team(skillinfo.GetCurSkill());
        }
    }
    void LearnSkill_Team(int skillid)
    {
        if (!mTeamskill.Contains(skillid))
        {
            mTeamskill.Add(skillid);
            for (int i = 0; i < mTeams.Count; i++)
            {
                mTeams[i].LearnSkill(skillid);
            }
        }
    }
    public void RemoveSkill_Team(int skillid)
    {
        if (mTeamskill.Contains(skillid))
        {
            mTeamskill.Remove(skillid);
            for (int i = 0; i < mTeams.Count; i++)
            {
                mTeams[i].RemoveSkill(skillid);
            }
        }
    }
    void RefreshSelfHeroInfo_Skill()
    {
        var learnskills = SkillMgr.Ins.GetLearnedSkillInfo();
        mSelfHeroInfo.skills.Clear();
        for (int i = 0; i < learnskills.Count; i++)
        {
            var skill = learnskills[i];
            if (skill.type == SkillMgr.SkillType.ZhuDong)
            {
                mSelfHeroInfo.skills.Add(skill.GetCurSkill());
            }
        }
    }
    AbyssHeroAI GetHeroAI(int id)
    {
        AbyssHeroAI re = null;
        for (int i = 0; i < mTeams.Count; i++)
        {
            re = mTeams[i];
            if (re.ClassID == id) return re;
        }
        return null;
    }

    //public int CalBuffPoint(int id)
    //{
    //    var skills = GetHeroSkills(id);
    //    int re = 0;
    //    if (skills != null)
    //    {
    //        for (int i = 0; i < skills.Count; i++)
    //        {
    //            int skillid = skills[i];
    //            var skillinfo = SkillMgr.Ins.GetSkillInfo_SkillId(skillid);
    //            if (skillinfo.type == SkillMgr.SkillType.ZhuDong)
    //            {
    //                if (SkillMgr.Ins.IsEvoSkill(skillinfo.id))
    //                {
    //                    re += (mBuffMgr.EVOSkill2Red * (skillinfo.GetLevel(skillid) + 1));
    //                }
    //                else
    //                {
    //                    re += (mBuffMgr.ZDSkill2Red * (skillinfo.GetLevel(skillid) + 1));
    //                }
    //            }
    //        }
    //    }
    //    if (re == 0) re = mBuffMgr.ZDSkill2Red;
    //    return re;
    //}

    //public void AddBuffPoint(int id)
    //{
    //    int v = CalBuffPoint(id);
    //    mBuffMgr.ChangePoint(AbyssBuffMgr.BUFF_RED, v);
    //}

    public bool GetPosState(int pos)
    {
        if (pos == 0) return true;
        if (pos == 1) return mPosState[0];
        if (pos == 2) return mPosState[1];
        return false;
    }

    public void SetPosStateUnlock(int pos)
    {
        if (pos == 1) mPosState[0] = true;
        else if (pos == 2) mPosState[1] = true;
    }
    public int GetPosUnlockCost(int pos)
    {
        if (pos == 0) return 0;
        if (pos == 1) return mPosUnlockCost[0];
        return mPosUnlockCost[1];
    }

    void SelfPlayerChange()
    {
        for (int i = 0; i < mTeams.Count; i++)
        {
            mTeams[i].SetPlayer(mSelf);
        }
    }
    void RefreshTeamQueue()
    {
        if (mTeams.Count == 1)
        {
            mTeams[0].SetFollowPos(Vector3.back * 2f );
        }
        else if (mTeams.Count == 2)
        {
            mTeams[0].SetFollowPos(Vector3.back * 2f + Vector3.right * 1.5f);
            mTeams[1].SetFollowPos(Vector3.back * 2f + Vector3.left * 1.5f);
        }
        if (OnTeamChange != null)
        {
            OnTeamChange();
        }

        List<int> tmpids = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            int id = GetHeroId_Index(i);
            if (id > 0)
            {
                tmpids.Add(id);              
            }
        }
        for (int i = 0; i < mMemberRecord.Count; i++)
        {
            int id = mMemberRecord[i];
            if (!tmpids.Contains(id))
            {
                tmpids.Add(id);
            }
        }
        mMemberRecord = tmpids;
    }

    SaveData_TeamData.TeamData __GetCurSaveInfodata = null;
    public SaveData_TeamData.TeamData GetCurSaveInfo()
    {
        if (__GetCurSaveInfodata == null)
        {
            __GetCurSaveInfodata = new SaveData_TeamData.TeamData();
        }
        LocalSave_Save(__GetCurSaveInfodata);
        return __GetCurSaveInfodata;
    }


    System.Text.StringBuilder __HandleStatistic_ToString_sb = new System.Text.StringBuilder();
    string HandleStatistic_ToString(List<int> ids)
    {
        if (ids == null) return "";
        __HandleStatistic_ToString_sb.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            __HandleStatistic_ToString_sb.Append(ids[i]);
            if (i < ids.Count - 1)
            {
                __HandleStatistic_ToString_sb.Append('/');
            }
        }
        return __HandleStatistic_ToString_sb.ToString();
    }

    public void HandleStatistic(string eventname)
    {
        if (!IsOpen) return;
        if (eventname == "lvmen_begin")
        {
            int id0 = GetHeroId_Index(0);
            if (id0 > 0)
            {
                StatisticMgr.AddParam("file_hero1", id0);
                var info = GetHeroInfo(id0, false);
                string skills = "";
                if(info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skill", skills);
            }

            int id1 = GetHeroId_Index(1);
            if (id1 > 0)
            {
                StatisticMgr.AddParam("file_hero2", id1);
                var info = GetHeroInfo(id1, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil2", info);
            }

            int id2 = GetHeroId_Index(2);
            if (id2 > 0)
            {
                StatisticMgr.AddParam("file_hero3", id2);
                var info = GetHeroInfo(id2, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil3", skills);
            }
            StatisticMgr.AddParam("Passive_skills", HandleStatistic_ToString(mTeamskill));

            var buffs = mBuffMgr.GetLearnBuff();
            List<int> n_red = new List<int>();
            List<int> n_blue = new List<int>();
            foreach (var item in buffs)
            {
                if (item.Cfg.Type == AbyssBuffMgr.BUFF_BULE)
                {
                    n_blue.Add(item.id);
                }
                else if (item.Cfg.Type == AbyssBuffMgr.BUFF_RED)
                {
                    n_red.Add(item.id);
                }
            }
            StatisticMgr.AddParam("red_buff", HandleStatistic_ToString(n_blue));
            StatisticMgr.AddParam("blue_buff", HandleStatistic_ToString(n_red));

            StatisticMgr.AddParam("red_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_RED));
            StatisticMgr.AddParam("blue_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_BULE));
        }
        else if (eventname == "lvmenroom_start")
        {
            int id0 = GetHeroId_Index(0);
            if (id0 > 0)
            {
                StatisticMgr.AddParam("hero1", id0);
                var info = GetHeroInfo(id0, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skill", skills);
            }

            int id1 = GetHeroId_Index(1);
            if (id1 > 0)
            {
                StatisticMgr.AddParam("hero2", id1);
                var info = GetHeroInfo(id1, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil2", skills);
            }

            int id2 = GetHeroId_Index(2);
            if (id2 > 0)
            {
                StatisticMgr.AddParam("hero3", id2);
                var info = GetHeroInfo(id2, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil3", skills);
            }
            StatisticMgr.AddParam("Passive_skills", HandleStatistic_ToString(mTeamskill));

            var buffs = mBuffMgr.GetLearnBuff();
            List<int> n_red = new List<int>();
            List<int> n_blue = new List<int>();
            foreach (var item in buffs)
            {
                if (item.Cfg.Type == AbyssBuffMgr.BUFF_BULE)
                {
                    n_blue.Add(item.id);
                }
                else if (item.Cfg.Type == AbyssBuffMgr.BUFF_RED)
                {
                    n_red.Add(item.id);
                }
            }
            StatisticMgr.AddParam("red_buff", HandleStatistic_ToString(n_blue));
            StatisticMgr.AddParam("blue_buff", HandleStatistic_ToString(n_red));

            StatisticMgr.AddParam("red_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_RED));
            StatisticMgr.AddParam("blue_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_BULE));
        }
        else if (eventname == "lvmen_end")
        {
            int id0 = GetHeroId_Index(0);
            if (id0 > 0)
            {
                StatisticMgr.AddParam("hero1", id0);
                var info = GetHeroInfo(id0, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skill", skills);
            }

            int id1 = GetHeroId_Index(1);
            if (id1 > 0)
            {
                StatisticMgr.AddParam("hero2", id1);
                var info = GetHeroInfo(id1, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil2", skills);
            }

            int id2 = GetHeroId_Index(2);
            if (id2 > 0)
            {
                StatisticMgr.AddParam("hero3", id2);
                var info = GetHeroInfo(id2, false);
                string skills = "";
                if (info != null)
                {
                    skills = HandleStatistic_ToString(info.skills);
                }
                StatisticMgr.AddParam("hero_skil3", skills);
            }
            StatisticMgr.AddParam("Passive_skills", HandleStatistic_ToString(mTeamskill));

            var buffs = mBuffMgr.GetLearnBuff();
            List<int> n_red = new List<int>();
            List<int> n_blue = new List<int>();
            foreach (var item in buffs)
            {
                if (item.Cfg.Type == AbyssBuffMgr.BUFF_BULE)
                {
                    n_blue.Add(item.id);
                }
                else if (item.Cfg.Type == AbyssBuffMgr.BUFF_RED)
                {
                    n_red.Add(item.id);
                }
            }
            StatisticMgr.AddParam("red_buff", HandleStatistic_ToString(n_blue));
            StatisticMgr.AddParam("blue_buff", HandleStatistic_ToString(n_red));

            StatisticMgr.AddParam("red_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_RED));
            StatisticMgr.AddParam("blue_icon_num", mBuffMgr.GetPoint(AbyssBuffMgr.BUFF_BULE));
        }
    }

    public void TeamLeaveBattleEffShow()
    {
        GameLogic.Release.SurvivorMonsterCtrl.PlayQuitBattlePreEff(mSelf);
        for (int i = 0; i < mTeams.Count; i++)
        {
            GameLogic.Release.SurvivorMonsterCtrl.PlayQuitBattlePreEff(mTeams[i]);
        }
    }


    #region LocalSave
    public void LocalSave_Load(int type)
    {
        SaveData_TeamData.TeamData data = LocalSave.Instance.mSaveDataEX.mTeamData.GetData(type);
        LocalSave_Load(data);
        mBuffMgr.LocalSave_Load(data);
    }
    void LocalSave_Load(SaveData_TeamData.TeamData data)
    {
        for (int i = 0; i < data.heroInfo.Count; i++)
        {
            SaveData_TeamData.HeroInfo info = data.heroInfo[i];
            var tmp = AddHeroInfo(info.id);
            tmp.state = info.state;
            tmp.level = info.level;
            tmp.exp = info.exp;
            tmp.hp = info.hp;
            tmp.skills.AddRange(info.skills);
        }

        TeamRolesTmp.Clear();
        TeamRolesTmp.AddRange(data.teamMember);

        mTeamskill.Clear();
        mTeamskill.AddRange(data.teamSkills);

        mPosState[0] = data.posstate[0];
        mPosState[1] = data.posstate[1];

        var dicreborn = GameLogic.Hold.BattleData.mDicRole2RebornUsedCount;
        dicreborn.Clear();
        foreach (var item in data.rolereborncount)
        {
            dicreborn.Add(item.Key, item.Value);
        }
        GameLogic.Hold.BattleData.mUnitRebornedCount = data.unitreborncount;
        mBuffMgr.LocalSave_Load(data);
    }

    public bool LocalSave_Save_BuffToNextLayer = false;
    public void LocalSave_Save(int type)
    {
        if (LocalSave_Save_BuffToNextLayer)
        {
            SkillMgr.Ins.RerollCount_Used = 0;
            SkillMgr.Ins.SkipCount_Used = 0;
            mBuffMgr.OnToNextLayer();
            LocalSave_Save_BuffToNextLayer = false;
        }
        SaveData_TeamData.TeamData data = LocalSave.Instance.mSaveDataEX.mTeamData.GetData(type);
        LocalSave_Save(data);       
        LocalSave.Instance.mSaveDataEX.RefreshSave();
    }
    public void LocalSave_Save(SaveData_TeamData.TeamData data)
    {
        data.heroInfo.Clear();
        for (int i = 0; i < mHeroInfos.Count; i++)
        {
            var info = mHeroInfos[i];
            SaveData_TeamData.HeroInfo tmp = new SaveData_TeamData.HeroInfo();
            tmp.id = info.id;
            tmp.state = info.state;
            tmp.level = info.level;
            tmp.exp = info.exp;
            tmp.hp = info.hp;
            tmp.skills.AddRange(info.skills);
            data.heroInfo.Add(tmp);
        }
        data.teamMember.Clear();
        if (SecretPlaceChooseCtrl.Temp_Save_UseRole != null && SecretPlaceChooseCtrl.Temp_Save_UseRole.Count > 0)
        {
            data.teamMember.AddRange(SecretPlaceChooseCtrl.Temp_Save_UseRole);
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                int id = GetHeroId_Index(i);
                if (id > 0)
                {
                    data.teamMember.Add(id);
                }
            }
        }
        data.teamMemberRecord.Clear();

        for (int i = 0; i < mMemberRecord.Count; i++)
        {
            data.teamMemberRecord.Add(mMemberRecord[i]);
        }

        data.teamSkills.Clear();
        data.teamSkills.AddRange(mTeamskill);

        var dicreborn = GameLogic.Hold.BattleData.mDicRole2RebornUsedCount;
        data.rolereborncount.Clear();
        foreach (var item in dicreborn)
        {
            data.rolereborncount.Add(item.Key, item.Value);
        }
        data.unitreborncount = GameLogic.Hold.BattleData.mUnitRebornedCount;
        data.posstate[0] = mPosState[0];
        data.posstate[1] = mPosState[1];

        mBuffMgr.LocalSave_Save(data);
    }
    public void ClearSave(int type)
    {
        LocalSave.Instance.mSaveDataEX.mTeamData.ClearData(type);
    }

    #endregion
    public class HeroInfo
    {
        public int id;
        public int state;//1=正常 2=死亡 3=callback
        public int level = 1;
        public int exp = 0;
        public long hp = 0;
        public List<int> skills = new List<int>();
    }
}
[Serializable]
public class SaveData_TeamData
{
    public TeamData mainSave = new TeamData();
    public TeamData temSave = new TeamData();

    public TeamData GetData(int type)
    {
        TeamData re = null;
        if (type == 0) re = temSave;
        else re = mainSave;

        for (int i = 0; i < re.heroInfo.Count; i++)
        {
            var info = re.heroInfo[i];
            if (info.skills.Count == 0)
            {
                var skillids = SkillMgr.Ins.GetMainSkill_SkillId(info.id);
                for (int j = 0; j < skillids.Count; j++)
                {
                    if (skillids[j] > 0)
                    {
                        info.skills.Add(skillids[j]);
                    }
                }
            }
        }
        if (re.posstate == null) re.posstate = new bool[2];

        int teammcnt = 0;
        if (re.teamMember.Count > 1 && re.teamMember[1] > 0) teammcnt++;
        if (re.teamMember.Count > 2 && re.teamMember[2] > 0) teammcnt++;
        if (teammcnt == 2)
        {
            if (!re.posstate[0]) re.posstate[0] = true;
            if (!re.posstate[1]) re.posstate[1] = true;
        }
        else if (teammcnt == 1)
        {
            if (!re.posstate[0] && !re.posstate[1]) re.posstate[0] = true;
        }
        return re;
    }

    public void ClearData(int type)
    {
        if (type == 0)
        {
            temSave = new TeamData();
        }
        else
        {
            mainSave = new TeamData();
        }
    }

    [Serializable]
    public class TeamData
    {
        public List<HeroInfo> heroInfo = new List<HeroInfo>();
        public List<int> teamMember = new List<int>();
        public List<int> teamSkills = new List<int>();
        public int buffBule = 0;
        public int buffRed = 0;
        public List<BuffInfo> teamBuffs = new List<BuffInfo>();
        public Dictionary<int, int> rolereborncount = new Dictionary<int, int>();
        public bool[] posstate = new bool[2];

        public int unitreborncount = 0;

        [JsonIgnore]
        public List<int> teamMemberRecord = new List<int>();


        public int GetHeroId_Index(int index)
        {
            if (teamMember.Count <= index) return -1;
            return teamMember[index];
        }

        public bool IsHeroUsed(int heroid)
        {
            return teamMember.Contains(heroid);
        }
        public int GetHeroState(int id)
        {
            for (int i = 0; i < heroInfo.Count; i++)
            {
                var info = heroInfo[i];
                if (info.id == id) return info.state;
            }
            return 1;
        }
    }

    [Serializable]
    public class HeroInfo
    {
        public int id;
        public int state;//1=正常 2=死亡 3=callback
        public int level;
        public int exp = 0;
        public long hp = 0;
        public List<int> skills = new List<int>();
    }
    [Serializable]
    public class BuffInfo
    {
        public int id;
        public int owner;//0=配置  1011=角色
        public int effectvalue;
        public void SetData(AbyssBuffMgr.BuffInfo info)
        {
            id = info.id;
            owner = info.owner;
            effectvalue = info.effectvalue;
        }
    }
}
public partial class SaveDataEX
{
    public SaveData_TeamData mTeamData = new SaveData_TeamData();

    public void InitTeamData(LocalSaveData_TeamData data)
    {
        if (data == null) return;

        if (data.MainSave != null)
        {
            if (data.MainSave.HeroInfo != null)
            {
                for (int i = 0; i < data.MainSave.HeroInfo.Count; i++)
                {
                    SaveData_TeamData.HeroInfo heroInfo = new SaveData_TeamData.HeroInfo();
                    heroInfo.id = data.MainSave.HeroInfo[i].Id;
                    heroInfo.state = data.MainSave.HeroInfo[i].State;
                    heroInfo.level = data.MainSave.HeroInfo[i].Level;
                    heroInfo.exp = data.MainSave.HeroInfo[i].Exp;
                    heroInfo.hp = data.MainSave.HeroInfo[i].Hp;
                    if (data.MainSave.HeroInfo[i].Skills != null)
                        heroInfo.skills = data.MainSave.HeroInfo[i].Skills.ToList();

                    mTeamData.mainSave.heroInfo.Add(heroInfo);
                }
            }
            if (data.MainSave.TeamMember != null)
                mTeamData.mainSave.teamMember = data.MainSave.TeamMember.ToList();
            if (data.MainSave.TeamSkills != null)
                mTeamData.mainSave.teamSkills = data.MainSave.TeamSkills.ToList();
            mTeamData.mainSave.buffBule = data.MainSave.BuffBule;
            mTeamData.mainSave.buffRed = data.MainSave.BuffRed;

            if (data.MainSave.TeamBuffs != null)
            {
                for (int i = 0; i < data.MainSave.TeamBuffs.Count; i++)
                {
                    mTeamData.mainSave.teamBuffs.Add(new SaveData_TeamData.BuffInfo
                    {
                        id = data.MainSave.TeamBuffs[i].Id,
                        owner = data.MainSave.TeamBuffs[i].Owner,
                        effectvalue = data.MainSave.TeamBuffs[i].Effectvalue,
                    });
                }
            }

            if (data.MainSave.Rolereborncount != null)
                mTeamData.mainSave.rolereborncount = data.MainSave.Rolereborncount.ToDictionary(p => p.Key, o => o.Value);
            if (data.MainSave.Posstate != null)
                mTeamData.mainSave.posstate = data.MainSave.Posstate.ToArray();
            mTeamData.mainSave.unitreborncount = data.MainSave.Unitreborncount;
            if (data.MainSave.TeamMemberRecord != null)
                mTeamData.mainSave.teamMemberRecord = data.MainSave.TeamMemberRecord.ToList();
        }
        
        if (data.TemSave != null)
        {
            if (data.TemSave.HeroInfo != null)
            {
                for (int i = 0; i < data.TemSave.HeroInfo.Count; i++)
                {
                    SaveData_TeamData.HeroInfo heroInfo = new SaveData_TeamData.HeroInfo();
                    heroInfo.id = data.TemSave.HeroInfo[i].Id;
                    heroInfo.state = data.TemSave.HeroInfo[i].State;
                    heroInfo.level = data.TemSave.HeroInfo[i].Level;
                    heroInfo.exp = data.TemSave.HeroInfo[i].Exp;
                    heroInfo.hp = data.TemSave.HeroInfo[i].Hp;
                    if (data.TemSave.HeroInfo[i].Skills != null)
                        heroInfo.skills = data.TemSave.HeroInfo[i].Skills.ToList();
                    mTeamData.temSave.heroInfo.Add(heroInfo);
                }
            }
            if(data.TemSave.TeamMember != null)
                mTeamData.temSave.teamMember = data.TemSave.TeamMember.ToList();
            if (data.TemSave.TeamSkills != null)
                mTeamData.temSave.teamSkills = data.TemSave.TeamSkills.ToList();
            mTeamData.temSave.buffBule = data.TemSave.BuffBule;
            mTeamData.temSave.buffRed = data.TemSave.BuffRed;
            if (data.TemSave.TeamBuffs != null)
            {
                for (int i = 0; i < data.TemSave.TeamBuffs.Count; i++)
                {
                    mTeamData.temSave.teamBuffs.Add(new SaveData_TeamData.BuffInfo
                    {
                        id = data.TemSave.TeamBuffs[i].Id,
                        owner = data.TemSave.TeamBuffs[i].Owner,
                        effectvalue = data.TemSave.TeamBuffs[i].Effectvalue,
                    });
                }
            }
            if(data.TemSave.Rolereborncount != null)
                mTeamData.temSave.rolereborncount = data.TemSave.Rolereborncount.ToDictionary(p => p.Key, o => o.Value);
            if (data.TemSave.Posstate != null)
                mTeamData.temSave.posstate = data.TemSave.Posstate.ToArray();
            mTeamData.temSave.unitreborncount = data.TemSave.Unitreborncount;
            if (data.TemSave.TeamMemberRecord != null)
                mTeamData.temSave.teamMemberRecord = data.TemSave.TeamMemberRecord.ToList();
        }
    }

    public LocalSaveData_TeamData GetTeamData()
    {
        LocalSaveData_TeamData data = new LocalSaveData_TeamData();
        data.MainSave = new LocalTeamData();
        data.TemSave = new LocalTeamData();
        for (int i = 0; i < mTeamData.mainSave.heroInfo.Count; i++)
        {
            var item = mTeamData.mainSave.heroInfo[i];
            LocalHeroInfo heroInfo = new LocalHeroInfo();
            heroInfo.Id = item.id;
            heroInfo.State = item.state;
            heroInfo.Level = item.level;
            heroInfo.Exp = item.exp;
            heroInfo.Hp = item.hp;
            heroInfo.Skills.AddRange(item.skills);
            data.MainSave.HeroInfo.Add(heroInfo);
        }
        data.MainSave.TeamMember.AddRange(mTeamData.mainSave.teamMember);
        data.MainSave.TeamSkills.AddRange(mTeamData.mainSave.teamSkills);
        data.MainSave.BuffBule = mTeamData.mainSave.buffBule;
        data.MainSave.BuffRed = mTeamData.mainSave.buffRed;
        for (int i = 0; i < mTeamData.temSave.teamBuffs.Count; i++)
        {
            var item = mTeamData.temSave.teamBuffs[i];
            data.MainSave.TeamBuffs.Add(new LocalBuffInfo
            {
                Id = item.id,
                Owner = item.owner,
                Effectvalue = item.effectvalue,
            });
        }
        data.MainSave.Rolereborncount.Add(mTeamData.mainSave.rolereborncount);
        data.MainSave.Posstate.AddRange(mTeamData.mainSave.posstate);
        data.MainSave.Unitreborncount = mTeamData.mainSave.unitreborncount;
        data.MainSave.TeamMemberRecord.AddRange(mTeamData.mainSave.teamMemberRecord);

        for (int i = 0; i < mTeamData.temSave.heroInfo.Count; i++)
        {
            var item = mTeamData.temSave.heroInfo[i];
            LocalHeroInfo heroInfo = new LocalHeroInfo();
            heroInfo.Id = item.id;
            heroInfo.State = item.state;
            heroInfo.Level = item.level;
            heroInfo.Exp = item.exp;
            heroInfo.Hp = item.hp;
            heroInfo.Skills.AddRange(item.skills);
            data.TemSave.HeroInfo.Add(heroInfo);
        }
        data.TemSave.TeamMember.AddRange(mTeamData.temSave.teamMember);
        data.TemSave.TeamSkills.AddRange(mTeamData.temSave.teamSkills);
        data.TemSave.BuffBule = mTeamData.temSave.buffBule;
        data.TemSave.BuffRed = mTeamData.temSave.buffRed;
        for (int i = 0; i < mTeamData.temSave.teamBuffs.Count; i++)
        {
            var item = mTeamData.temSave.teamBuffs[i];
            data.TemSave.TeamBuffs.Add(new LocalBuffInfo
            {
                Id = item.id,
                Owner = item.owner,
                Effectvalue = item.effectvalue,
            });
        }
        data.TemSave.Rolereborncount.Add(mTeamData.temSave.rolereborncount);
        data.TemSave.Posstate.AddRange(mTeamData.temSave.posstate);
        data.TemSave.Unitreborncount = mTeamData.temSave.unitreborncount;
        data.TemSave.TeamMemberRecord.AddRange(mTeamData.temSave.teamMemberRecord);
        return data;
    }
}
