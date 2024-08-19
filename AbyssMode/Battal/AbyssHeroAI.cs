using Dxx.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbyssHeroAI : EntityHero
{
    EntityHero mPlayer;
    Vector3 mTargetPos;
    JoyData m_MoveData = default(JoyData);
    int mState = -1;//1=站立 2=追 3=卡住
    float DisFromTarget
    {
        get
        {          
            float dis = Vector3.Distance(TargetWorldPos, position);
            return dis;
        }
    }

    Vector3 TargetWorldPos
    {
        get
        {
            return mTargetPos + mPlayer.position;
        }
    }

    protected override void OnInit()
    {
        base.OnInit();
        SetCollider(false);
        m_MoveData.SetMoveJoy();
        m_MoveData.action = AnimationCtrlBase.Run;
        //(mAniCtrlBase as AnimationCtrlHero).SetAnimaorIsMovingAttack(true);
    }

    protected override void OnDeInitLogic()
    {
        base.OnDeInitLogic();
    }

    public void StartGame()
    {
        GoState(1);
    }
  
    public void SetPlayer(EntityHero player)
    {
        mPlayer = player;
    }
    
    public void SetFollowPos(Vector3 pos)
    {
        mTargetPos = pos;
    }
    protected override void UpdateProcess(float delta)
    {
        base.UpdateProcess(delta);
        UpdateState();
    }


    Vector3 __state_parma_v30;
    float __state_param_f0;
    int __state_param_i0;
    void GoState(int state)
    {
        if (mState == state) return;
        int oldstate = mState;
        mState = state;
        if (mState == 1)
        {
            __state_param_i0 = oldstate == 2 ? 1 : 0;
            m_MoveCtrl.AIMoveEnd(m_MoveData);
            (mAniCtrlBase as AnimationCtrlHero).SetAnimaorIsMovingAttack(false);
        }
        else if (mState == 2)
        {
            __state_param_i0 = 0;
            m_EntityData.SetMoveSpeedScale(1f);
            m_MoveCtrl.AIMoveStart(m_MoveData);
            (mAniCtrlBase as AnimationCtrlHero).SetAnimaorIsMovingAttack(true);
        }
        else if (mState == 3)
        {
            m_MoveCtrl.AIMoveEnd(m_MoveData);
            (mAniCtrlBase as AnimationCtrlHero).SetAnimaorIsMovingAttack(false);
        }
    }

    float GetMoveStep(bool unscale = false)
    {
        return m_EntityData.GetSpeed(unscale) * Time.deltaTime;
    }

    float GetMinSpeedScale()
    {
        return mPlayer.m_EntityData.GetSpeed() / m_EntityData.GetSpeed(true);
    }

    void UpdateState()
    {
        if (mState == 1)
        {
            if (__state_param_i0 == 2)
            {
                SetPosition(TargetWorldPos);
                __state_param_i0 = 0;
            }
            else if(__state_param_i0 == 1)
            {
                __state_param_i0 = 2;
                return;
            }
            float stepmove = GetMoveStep();
            if (DisFromTarget > stepmove * 4f)
            {
                GoState(2);
            }
        }
        else if (mState == 2)
        {
            float dis = DisFromTarget;
            float stepmove = GetMoveStep();
            if (dis < stepmove)
            {
                GoState(1);
                return;
            }
            if (dis > 10f)
            {
                Vector3 dir = mPlayer.position - position;
                Vector3 movepos = dir * 0.1f + position;
                SetPosition(movepos);
            }
            else
            {
                if (__state_param_i0 > 1)
                {
                    if (RaycastWall())
                    {
                        GoState(3);
                        return;
                    }
                }
                __state_param_i0++;
            }
            UpdateRunTargetSpeed();
            Run2Target();
        }
        else if (mState == 3)
        {
            if (DisFromTarget > 10f)
            {
                Vector3 dir = mPlayer.position - position;
                Vector3 movepos = dir * 0.1f + position;
                SetPosition(movepos);
                GoState(2);
                return;
            }
            if (!RaycastWall())
            {
                GoState(2);
            }
        }
    }
    bool RaycastWall()
    {
        Vector3 dircurpos = (TargetWorldPos - position).normalized;
        RaycastHit hitinfo;
        if (Physics.Raycast(position, dircurpos, out hitinfo, 0.5f, GetLayMask()))
        {
            float angle = Vector3.Angle(-dircurpos, hitinfo.normal);

            if (Mathf.Abs(angle) < 20f)
            {
                return true;
            }
        }
        return false;
    }
    int __masklay = 0;
    protected int GetLayMask()
    {
        if (__masklay == 0)
        {
            __masklay = 1 << LayerManager.Player | 1 << LayerManager.Bullet | 1 << LayerManager.PlayerAbsorbImme | 1 << LayerManager.Goods | LayerManager.PlayerAbsorb;
            __masklay = ~__masklay;
        }
        return __masklay;
    }

    void UpdateRunTargetSpeed()
    {      
        float dis = DisFromTarget - GetMoveStep();
        float speedscale = GetMinSpeedScale() * Mathf.Lerp(1f, 2f, dis / 5f);
        m_EntityData.SetMoveSpeedScale(speedscale);
    }
    void Run2Target()
    {
        Vector3 targetpos = mTargetPos + mPlayer.position;
        Vector3 dir = targetpos - position;
        this.m_MoveData.angle = Utils.getAngle(dir);
        float x = MathDxx.Sin(this.m_MoveData.angle);
        float z = MathDxx.Cos(this.m_MoveData.angle);
        this.m_MoveData.direction.Set(x, 0f, z);
        m_MoveCtrl.AIMoving(m_MoveData);
    }



}
