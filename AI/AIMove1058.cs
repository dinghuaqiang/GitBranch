using Dxx.Util;
using UnityEngine;

public class AIMove1058 : AIMoveBase
{
    public AIMove1058(EntityBase entity) : base(entity)
    {
    }
    protected override void OnInitBase()
    {

    }

    protected override void OnUpdate()
    {
        if (Time.frameCount % 2 == 0)
        {
            this.m_MoveData.angle = Utils.getAngle(GameLogic.Self.position - this.m_Entity.position);
            float x = MathDxx.Sin(this.m_MoveData.angle);
            float z = MathDxx.Cos(this.m_MoveData.angle);
            this.m_MoveData.direction.Set(x, 0f, z);
            this.m_Entity.m_AttackCtrl.RotateHero(this.m_MoveData.angle, this.m_MoveData.direction);
        }
        this.m_Entity.m_MoveCtrl.AIMoveStart(this.m_MoveData);
        this.m_Entity.m_MoveCtrl.AIMoving(this.m_MoveData);
    }

    protected override void OnEnd()
    {
        this.m_Entity.m_MoveCtrl.AIMoveEnd(this.m_MoveData);
    }

}
