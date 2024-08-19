using System;
using TableTool;

public abstract class AIMoveBase : ActionBasic.ActionUIBase
{
    public AIMoveBase(EntityBase entity)
    {
        this.m_MoveData.SetMoveJoy();
        this.m_MoveData.action = AnimationCtrlBase.Run;
        this.ClassName = base.GetType().ToString();
        string s = this.ClassName.Substring(this.ClassName.Length - 4, 4);
        int.TryParse(s, out this.ClassID);
        //this.Data = LocalModelManager.Instance.Operation_move.GetBeanById(this.ClassID);
        this.name = this.ClassName;
        this.m_Entity = entity;
    }

    protected sealed override void OnInit()
    {
        if (!this.m_Entity)
        {
            base.End();
            return;
        }
        EntityBase entity = this.m_Entity;
        entity.OnDizzy = (Action<bool>)Delegate.Combine(entity.OnDizzy, new Action<bool>(this.OnDizzy));
        this.OnInitBase();
    }

    private void OnDizzy(bool value)
    {
        if (value)
        {
            base.End();
        }
    }

    protected override void OnEnd1()
    {
        EntityBase entity = this.m_Entity;
        entity.OnDizzy = (Action<bool>)Delegate.Remove(entity.OnDizzy, new Action<bool>(this.OnDizzy));
    }

    protected abstract void OnInitBase();

    public static ConditionBase GetConditionTime(int time)
    {
        return new ConditionTime
        {
            time = (float)time / 1000f
        };
    }

    public static ConditionBase GetConditionRandomTime(int min, int max)
    {
        return new ConditionTime
        {
            time = GameLogic.Random((float)min / 1000f, (float)max / 1000f)
        };
    }

    protected override void OnForceEnd()
    {
    }

    //protected Operation_move Data;

    public string ClassName;

    public int ClassID;

    protected JoyData m_MoveData = default(JoyData);
}
