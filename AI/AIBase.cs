using Dxx.Util;
using Pbmsg;
using System;
using System.Collections.Generic;
using TableTool;
using UnityEngine;

public class AIBase : ActionBasic
{
    public void SetEntity(EntityBase entity)
    {
        this.m_Entity = entity;
        this.m_MonsterEntity = (entity as EntityMonsterBase);
    }

    public int ClassID
    {
        get
        {
            return this.pClassID;
        }
    }

    protected sealed override void OnInit1()
    {
        this.ClassName = base.GetType().ToString();
        string s = this.ClassName.Substring(this.ClassName.Length - 4, 4);
        int.TryParse(s, out this.pClassID);
        this.actionTime = Updater.AliveTime;
        this.mRoomTime = GameLogic.Random(0.5f, 0.7f);
        this.mCreateNewTime = 0.3f;
        if (this.m_Entity.IsElite)
        {
            this.OnElite();
        }
        this.OnInitOnce();
        this.OnInit();
        if (this.bReRandom)
        {
            base.AddAction(this.GetActionDelegate(new Action(this.ReRandomAI)));
        }
    }

    protected virtual void OnInitOnce()
    {
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnElite()
    {
    }

    protected sealed override void OnDeInit()
    {
        base.OnDeInit();
        this.RemoveAttack();
        this.RemoveCurrentAction();
        this.OnAIDeInit();
    }

    protected virtual void OnAIDeInit()
    {
    }

    protected void ReRandomAI()
    {
        base.ActionClear();
        this.OnInit();
        base.AddAction(this.GetActionDelegate(new Action(this.ReRandomAI)));
    }

    protected override void OnUpdate(float delta)
    {
        if (this.m_Entity.IsNull())
        {
            return;
        }
        if (this.m_Entity.IsUnityNull())
        {
            this.m_Entity = null;
            return;
        }
        if (!this.m_Entity.gameObject.activeInHierarchy || this.m_Entity.GetIsDead())
        {
            return;
        }
        if (this.IsDelayTime)
        {
            if (this.m_Entity.bDivide || this.m_Entity.bCall)
            {
                if (Updater.AliveTime - this.actionTime < this.mCreateNewTime)
                {
                    return;
                }
            }
            else if (Updater.AliveTime - this.actionTime < this.mRoomTime)
            {
                return;
            }
        }
        if (this.m_Entity.m_EntityData.IsDizzy())
        {
            return;
        }
        
        // 并行行为之间的次序
        if (this.parallelActionsList.Count > 0)
        {
            for (var i = 0; i < parallelActionsList.Count; i++)
            {
                parallelActionsList[i].Init();
                parallelActionsList[i].Update();
                if (this.parallelActionsList[i].IsEnd)
                {
                    this.parallelActionsList[i].Reset();
                }
                if (parallelActionsList[i].isActive)
                {
                    break;
                }
            }
        }
        
        if (this.actionCount > 0)
        {
            ActionBasic.ActionBase actionBase = this.actionList[this.actionIndex];
            if (ProcessParActions())
            {
                if (!OnActionEndFunc(actionBase))
                {
                    actionBase.ForceEnd();
                    actionBase.Reset();
                }
                
                return;
            }
            
            int actionIndex = this.actionIndex;
            if (actionIndex == this.actionIndex && actionBase.IsEnd)
            {
                this.actionIndex++;
                if (this.actionIndex >= this.actionCount)
                {
                    for (int i = 0; i < this.actionCount; i++)
                    {
                        this.actionList[i].Reset();
                    }
                }
                this.actionIndex %= this.actionCount;
                actionBase = this.actionList[this.actionIndex];
                actionIndex = this.actionIndex;
            }
            actionBase.Init();
            actionBase.Update();
            if (actionIndex == this.actionIndex && actionBase.IsEnd)
            {
                this.actionIndex++;
                if (this.actionIndex >= this.actionCount)
                {
                    for (int j = 0; j < this.actionCount; j++)
                    {
                        this.actionList[j].Reset();
                    }
                }
                this.actionIndex %= this.actionCount;
            }
        }
    }

    /// <summary>
    /// 某一行为结束后
    /// </summary>
    /// <returns></returns>
    protected virtual bool OnActionEndFunc(ActionBasic.ActionBase actionBase)
    {
        return false;
    }

    /// <summary>
    /// 处理并行行为
    /// </summary>
    /// <returns></returns>
    protected virtual bool ProcessParActions()
    {
        if (this.parallelActionsList.Count > 0)
        {
            for (var i = 0; i < parallelActionsList.Count; i++)
            {
                if (parallelActionsList[i].canInvoke)
                {
                    parallelActionsList[i].PareInvoke();
                }
                if (parallelActionsList[i].isActive)
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    protected ActionBasic.ActionBase GetActionRotate(float angle)
    {
        return new AIBase.ActionRotate
        {
            m_Entity = this.m_Entity,
            angle = angle
        };
    }

    protected ActionBasic.ActionBase GetActionRotateToEntity(EntityBase target)
    {
        return new AIBase.ActionRotateToEntity
        {
            m_Entity = this.m_Entity,
            target = target
        };
    }

    protected ActionBasic.ActionBase GetActionRotateToPos(Vector3 pos)
    {
        return new AIBase.ActionRotateToPos
        {
            m_Entity = this.m_Entity,
            pos = pos
        };
    }

    protected ActionBasic.ActionWait GetActionWait(string name, int waitTime)
    {
        return new ActionBasic.ActionWait
        {
            name = name,
            waitTime = (float)waitTime / 1000f,
            m_Entity = this.m_Entity
        };
    }

    protected AIBase.ActionWaitRandom GetActionWaitRandom(string name, int min, int max)
    {
        return new AIBase.ActionWaitRandom
        {
            name = name,
            min = min,
            max = max,
            m_Entity = this.m_Entity
        };
    }
    protected ActionBasic.ActionDelegate GetActionDelegate(Action action)
    {
        return new ActionBasic.ActionDelegate
        {
            action = action
        };
    }

    protected ActionBasic.ActionBase GetActionWaitDelegate(int time, Action action)
    {
        AIBase.ActionSequence actionSequence = new AIBase.ActionSequence
        {
            m_Entity = this.m_Entity
        };
        actionSequence.AddAction(this.GetActionWait(string.Empty, time));
        actionSequence.AddAction(this.GetActionDelegate(action));
        return actionSequence;
    }

    protected ActionBasic.ActionDelegate GetActionRemoveMove()
    {
        return this.GetActionDelegate(delegate
        {
            this.RemoveMove();
        });
    }

    public void RemoveMove()
    {
        if (this.actionCount > 0)
        {
            ActionBasic.ActionBase actionBase = this.actionList[this.actionIndex];
            if (actionBase is AIMoveBase)
            {
                actionBase.ForceEnd();
            }
        }
    }

    protected void RemoveCurrentAction()
    {
        if (this.actionCount > 0)
        {
            ActionBasic.ActionBase actionBase = this.actionList[this.actionIndex];
            actionBase.ForceEnd();
        }
    }
    protected bool GetIsAlive()
    {
        return this.m_Entity && !this.m_Entity.GetIsDead();
    }

    public void DeadBefore()
    {
        this.OnDeadBefore();
    }

    protected virtual void OnDeadBefore()
    {
    }

    protected override void OnActionClear()
    {
        this.actionIndex = 0;
    }

    private void RemoveAttack()
    {
        if (this.mEntityAttack != null)
        {
            this.mEntityAttack.UnInstall();
            this.mEntityAttack = null;
        }
    }

    public void Attack(int AttackID, bool bRotate)
    {
        this.RemoveMove();
        this.RemoveAttack();
        this.mEntityAttack = new EntityAttack();
        this.mEntityAttack.SetRotate(bRotate);
        this.mEntityAttack.Init(this.m_Entity, AttackID);
    }

    public void AttackSpecial(int AttackID, bool bRotate)
    {
        this.RemoveMove();
        Type type = Type.GetType(Utils.GetString(new object[]
        {
            "EntityAttack",
            AttackID
        }));
        this.mEntityAttack = (type.Assembly.CreateInstance(Utils.GetString(new object[]
        {
            "EntityAttack",
            AttackID
        })) as EntityAttackBase);
        this.mEntityAttack.SetRotate(bRotate);
        this.mEntityAttack.Init(this.m_Entity, AttackID);
    }

    public bool GetAttackEnd()
    {
        return (this.mEntityAttack == null || this.mEntityAttack.GetIsEnd()) && !this.m_Entity.m_AttackCtrl.GetAttacking();
    }

    protected ActionBasic.ActionDelegate GetActionRemoveAttack()
    {
        return this.GetActionDelegate(delegate
        {
            this.RemoveAttack();
        });
    }

    protected AIBase.ActionSequence GetActionAttackWait(int attackID, int waittime, int waitmaxtime = -1)
    {
        AIBase.ActionSequence actionSequence = new AIBase.ActionSequence
        {
            m_Entity = this.m_Entity
        };
        actionSequence.AddAction(this.GetActionAttack(string.Empty, attackID, true));
        if (waitmaxtime == -1)
        {
            waitmaxtime = waittime;
        }
        actionSequence.AddAction(this.GetActionWaitRandom(string.Empty, waittime, waitmaxtime));
        return actionSequence;
    }

    protected AIBase.ActionAttack GetActionAttack(string name, int attackId, bool rotate = true)
    {
        return new AIBase.ActionAttack
        {
            name = name,
            attackId = attackId,
            bAttackSpecial = false,
            bRotate = rotate,
            m_AIBase = this,
            m_Entity = this.m_Entity
        };
    }

    protected AIBase.ActionAttack GetActionAttackSpecial(string name, int attackId, bool rotate = true)
    {
        return new AIBase.ActionAttack
        {
            name = name,
            attackId = attackId,
            bAttackSpecial = true,
            bRotate = rotate,
            m_AIBase = this,
            m_Entity = this.m_Entity
        };
    }

    protected void InitCallData(AIBase.CallData data)
    {
        if (this.mCallList.ContainsKey(data.CallID))
        {
            this.mCallList[data.CallID] = data;
        }
        else
        {
            this.mCallList.Add(data.CallID, data);
        }
    }

    protected void InitCallData(int callid, int alivecount, int count, int percount, int radiusmin, int radiusmax)
    {
        AIBase.CallData callData;
        if (this.mCallList.TryGetValue(callid, out callData))
        {
            callData.CallID = callid;
            callData.MaxAliveCount = alivecount;
            callData.MaxCount = count;
            callData.perCount = percount;
            callData.radiusmin = radiusmin;
            callData.radiusmax = radiusmax;
        }
        else
        {
            callData = new AIBase.CallData(callid, alivecount, count, percount, radiusmin, radiusmax);
            this.mCallList.Add(callid, callData);
        }
    }

    protected void AddCallCount(int callid)
    {
        AIBase.CallData callData;
        if (this.mCallList.TryGetValue(callid, out callData))
        {
            callData.AddCall();
        }
    }

    protected void RemoveCallCount(int callid)
    {
        AIBase.CallData callData;
        if (this.mCallList.TryGetValue(callid, out callData))
        {
            callData.RemoveCall();
        }
    }

    protected bool GetCanCall(object callid)
    {
        int key = (int)callid;
        AIBase.CallData callData;
        return this.mCallList.TryGetValue(key, out callData) && callData.GetCanCall();
    }

    protected int GetCallCount(int callid)
    {
        AIBase.CallData callData;
        if (this.mCallList.TryGetValue(callid, out callData))
        {
            return callData.GetCallCount();
        }
        return 0;
    }

    protected int GetAliveCount(int callid, bool over = false)
    {
        AIBase.CallData callData;
        if (this.mCallList.TryGetValue(callid, out callData))
        {
            return callData.CurAliveCount;
        }
        return 0;
    }

    protected ActionBasic.ActionBase GetActionCallInternal(int entityId, Action<AIBase.ActionCall.ActionCallData> call)
    {
        AIBase.ActionSequence actionSequence = new AIBase.ActionSequence();
        actionSequence.ConditionBase = new Func<bool>(this.GetIsAlive);
        actionSequence.AddAction(new ActionBasic.ActionDelegate
        {
            action = delegate ()
            {
                this.m_Entity.m_AniCtrl.SendEvent(AnimationCtrlBase.Call, false);
            }
        });
        float animationTime = this.m_Entity.mAniCtrlBase.GetAnimationTime(AnimationCtrlBase.Call);
        actionSequence.AddAction(new ActionBasic.ActionWait
        {
            waitTime = animationTime * 0.4f
        });
        AIBase.ActionCall actionCall = new AIBase.ActionCall();
        actionCall.InitData(entityId);
        actionCall.action = call;
        actionSequence.AddAction(actionCall);
        actionSequence.AddAction(new ActionBasic.ActionWait
        {
            waitTime = animationTime * 0.6f
        });
        return actionSequence;
    }

    private bool IsCallStand(int entityid)
    {
        return LocalModelManager.Instance.Character_Char.GetBeanById(entityid).Speed == 0;
    }

    public EntityBase m_Entity;

    public EntityMonsterBase m_MonsterEntity;

    protected string ClassName;

    private int pClassID;

    private float actionTime;

    private EntityAttackBase mEntityAttack;

    protected float mRoomTime;

    private float mCreateNewTime;

    private float mStartTime;

    protected bool IsDelayTime = true;

    protected bool bReRandom;

    private Dictionary<int, AIBase.CallData> mCallList = new Dictionary<int, AIBase.CallData>();

    public class ActionChoose : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.bResult = this.Condition();
            this.ExcuteResultInit();
        }

        private void ExcuteResultInit()
        {
            if (this.bResult)
            {
                if (this.ResultTrue != null)
                {
                    this.ResultTrue.Init();
                    if (this.ResultTrue.IsEnd)
                    {
                        base.End();
                    }
                }
                else
                {
                    base.End();
                }
            }
            else if (this.ResultFalse != null)
            {
                this.ResultFalse.Init();
                if (this.ResultFalse.IsEnd)
                {
                    base.End();
                }
            }
            else
            {
                base.End();
            }
        }

        private void ExcuteResultUpdate()
        {
            if (this.bResult)
            {
                if (this.ResultTrue != null)
                {
                    this.ResultTrue.Update();
                    if (this.ResultTrue.IsEnd)
                    {
                        base.End();
                    }
                }
            }
            else if (this.ResultFalse != null)
            {
                this.ResultFalse.Update();
                if (this.ResultFalse.IsEnd)
                {
                    base.End();
                }
            }
        }

        protected override void OnForceEnd()
        {
            if (this.bResult)
            {
                if (this.ResultTrue != null)
                {
                    this.ResultTrue.ForceEnd();
                }
            }
            else if (this.ResultFalse != null)
            {
                this.ResultFalse.ForceEnd();
            }
        }

        protected override void OnUpdate()
        {
            this.ExcuteResultUpdate();
        }

        public Func<bool> Condition;

        public ActionBasic.ActionBase ResultTrue;

        public ActionBasic.ActionBase ResultFalse;

        private bool bResult;
    }

    public class ActionSequence : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.index = 0;
        }

        protected override void OnUpdate()
        {
            if (this.index < this.count)
            {
                ActionBasic.ActionBase actionBase = this.list[this.index];
                actionBase.Init();
                actionBase.Update();
                if (this.list[this.index].IsEnd)
                {
                    this.index++;
                }
            }
            else
            {
                base.End();
            }
        }

        protected override void OnForceEnd()
        {
            if (this.index < this.count)
            {
                ActionBasic.ActionBase actionBase = this.list[this.index];
                actionBase.ForceEnd();
            }
        }

        public void AddAction(ActionBasic.ActionBase action)
        {
            this.list.Add(action);
            this.count++;
        }

        public List<ActionBasic.ActionBase> list = new List<ActionBasic.ActionBase>();

        private int count;

        private int index;
    }

    public class ActionWaitRandom : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.startTime = Updater.AliveTime;
            this.waitTime = (float)this.GetRandomInt(this.min, this.max) / 1000f;
        }

        protected override void OnUpdate()
        {
            if (Updater.AliveTime - this.startTime >= this.waitTime)
            {
                base.End();
            }
        }

        private int GetRandomInt(int min, int max)
        {
            return GameLogic.Random(min, max);
        }

        protected override void OnForceEnd()
        {
        }

        public int min;

        public int max;

        private float startTime;

        private float waitTime;
    }

    public class ActionMove : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            if (this.action != null)
            {
                this.action(this.moveId);
            }
            base.End();
        }

        protected override void OnForceEnd()
        {
        }

        public int moveId;

        public Action<int> action;
    }

    public class ActionDivide : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            if (this.action != null)
            {
                this.action(this.entityId, this.count);
            }
            base.End();
        }

        protected override void OnForceEnd()
        {
        }

        public int entityId;

        public int count;

        public Action<int, int> action;
    }

    public class ActionCall : ActionBasic.ActionBase
    {
        public void InitData(int entityId)
        {
            this.data = new AIBase.ActionCall.ActionCallData
            {
                entityId = entityId
            };
        }

        protected override void OnInit()
        {
            if (this.action != null)
            {
                this.action(this.data);
            }
            base.End();
        }

        protected override void OnForceEnd()
        {
        }

        private AIBase.ActionCall.ActionCallData data;

        public Action<AIBase.ActionCall.ActionCallData> action;

        public class ActionCallData
        {
            public int entityId;
        }
    }

    public class ActionRotate : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.bRotate = true;
        }

        protected override void OnUpdate()
        {
            if (this.bRotate)
            {
                this.m_Entity.m_AttackCtrl.RotateHero(this.angle);
                this.bRotate = false;
            }
            if (this.m_Entity.m_AttackCtrl.RotateOver())
            {
                base.End();
            }
        }

        protected override void OnForceEnd()
        {
        }

        public float angle;

        private bool bRotate;
    }

    public class ActionRotateToEntity : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.bRotate = true;
        }

        protected override void OnUpdate()
        {
            if (this.bRotate)
            {
                this.m_Entity.m_AttackCtrl.RotateHero(Utils.getAngle(this.target.position - this.m_Entity.position));
                this.bRotate = false;
            }
            if (this.m_Entity.m_AttackCtrl.RotateOver())
            {
                base.End();
            }
        }

        protected override void OnForceEnd()
        {
        }

        public EntityBase target;

        private bool bRotate;
    }

    public class ActionRotateToPos : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.bRotate = true;
        }

        protected override void OnUpdate()
        {
            if (this.bRotate)
            {
                this.m_Entity.m_AttackCtrl.RotateHero(Utils.getAngle(this.pos - this.m_Entity.position));
                this.bRotate = false;
            }
            if (this.m_Entity.m_AttackCtrl.RotateOver())
            {
                base.End();
            }
        }

        protected override void OnForceEnd()
        {
        }

        public Vector3 pos;

        private bool bRotate;
    }

    public class ActionRotateTime : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.mTime = this.time;
        }

        protected override void OnUpdate()
        {
            if (this.mTime > 0f)
            {
                this.m_Entity.m_AttackCtrl.RotateHero(Utils.getAngle(this.target.position - this.m_Entity.position));
                this.mTime -= Updater.delta;
            }
            else
            {
                base.End();
            }
        }

        protected override void OnForceEnd()
        {
        }

        public EntityBase target;

        public float time;

        private float mTime;
    }

    public class ActionAttack : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.bPlayAttack = false;
            this.m_AIBase.RemoveAttack();
        }

        protected override void OnUpdate()
        {
            if (this.m_Entity == null)
            {
                base.End();
                return;
            }
            if (this.m_Entity != null && !this.bPlayAttack)
            {
                if (this.bRotate)
                {
                    this.m_Entity.m_AttackCtrl.RotateUpdate(this.m_Entity.m_HatredTarget);
                }
                if (this.m_Entity.m_AttackCtrl.RotateOver() || !this.bRotate)
                {
                    this.bPlayAttack = true;
                    this.Attack();
                }
            }
            if (this.bPlayAttack)
            {
                this.test_time += Updater.delta;
                if (this.test_time > 1f)
                {
                    this.test_time -= 1f;
                }
                if (this.m_AIBase.GetAttackEnd())
                {
                    base.End();
                }
            }
        }

        private void Attack()
        {
            if (!this.bAttackSpecial)
            {
                this.m_AIBase.Attack(this.attackId, this.bRotate);
            }
            else
            {
                this.m_AIBase.AttackSpecial(this.attackId, this.bRotate);
            }
        }

        protected override void OnForceEnd()
        {
        }

        public int attackId;

        public AIBase m_AIBase;

        public bool bAttackSpecial;

        public bool bRotate = true;

        private bool bPlayAttack;

        private float test_time;
    }

    public class ActionChooseRandom : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
        }

        protected override void OnUpdate()
        {
            if (this.currentIndex == -1)
            {
                this.currentIndex = this.GetRandomWeight();
            }
            ActionBasic.ActionBase actionBase = this.actionList[this.currentIndex];
            
            actionBase.Init();
            actionBase.Update();
            if (actionBase.IsEnd)
            {
                base.End();
                this.currentIndex = -1;
            }
        }

        public void AddAction(int weight, ActionBasic.ActionBase action)
        {
            this.weightList.Add(weight);
            this.allWeight += weight;
            this.actionList.Add(action);
            this.actionCount++;
        }

        protected virtual int GetRandomWeight()
        {
            int num = GameLogic.Random(0, this.allWeight);
            int num2 = 0;
            for (int i = 0; i < this.actionCount; i++)
            {
                num2 += this.weightList[i];
                if (num < num2)
                {
                    return i;
                }
            }
            return 0;
        }

        protected override void OnForceEnd()
        {
            if (this.currentIndex >= 0)
            {
                ActionBasic.ActionBase actionBase = this.actionList[this.currentIndex];
                actionBase.ForceEnd();
            }
        }

        private List<ActionBasic.ActionBase> actionList = new List<ActionBasic.ActionBase>();

        public List<int> weightList = new List<int>();

        protected int allWeight;

        protected int actionCount;

        protected int currentIndex = -1;
    }

    public class ActionChooseIf : ActionBasic.ActionBase
    {
        protected override void OnInit()
        {
            this.index = this.count;
            for (int i = 0; i < this.count; i++)
            {
                ActionBasic.ActionBase actionBase = this.list[i];
                if (actionBase.ConditionBase == null || actionBase.ConditionBase())
                {
                    this.index = i;
                    break;
                }
            }
        }

        protected override void OnUpdate()
        {
            if (this.index < this.count)
            {
                ActionBasic.ActionBase actionBase = this.list[this.index];
                if (actionBase.IsEnd)
                {
                    base.End();
                    return;
                }
                actionBase.Init();
                actionBase.Update();
                if (actionBase.IsEnd)
                {
                    base.End();
                }
            }
            else
            {
                base.End();
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            if (this.index < this.count)
            {
                ActionBasic.ActionBase actionBase = this.list[this.index];
                actionBase.Reset();
            }
        }

        protected override void OnForceEnd()
        {
            if (this.index >= 0 && this.index < this.count)
            {
                ActionBasic.ActionBase actionBase = this.list[this.index];
                actionBase.ForceEnd();
            }
        }

        public void AddAction(ActionBasic.ActionBase action)
        {
            this.list.Add(action);
            this.count++;
        }

        private List<ActionBasic.ActionBase> list = new List<ActionBasic.ActionBase>();

        private int count;

        private int index;
    }

    public class CallData
    {
        public CallData(int callid, int alivecount, int count, int percount, int radiusmin, int radiusmax)
        {
            this.CallID = callid;
            this.MaxAliveCount = alivecount;
            this.MaxCount = count;
            this.perCount = percount;
            this.radiusmin = radiusmin;
            this.radiusmax = radiusmax;
        }

        public void AddCall()
        {
            this.CurAllCount++;
            this.CurAliveCount++;
        }

        public void RemoveCall()
        {
            this.CurAliveCount--;
        }

        public bool GetCanCall()
        {
            return this.CurAliveCount < this.MaxAliveCount && this.CurAllCount < this.MaxCount;
        }

        public int GetCallCount()
        {
            int num = this.perCount;
            if (this.MaxCount - this.CurAllCount < num)
            {
                num = (this.MaxCount = this.CurAllCount);
            }
            if (this.MaxAliveCount - this.CurAliveCount < num)
            {
                num = this.MaxAliveCount - this.CurAliveCount;
            }
            return num;
        }

        public int CallID;

        public int MaxAliveCount;

        public int MaxCount;

        public int perCount;

        public int radiusmin;

        public int radiusmax;

        public int CurAliveCount;

        public int CurAllCount;
    }
}
