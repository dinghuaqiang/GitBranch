using Dxx.Util;
using UnityEngine;

public class AIMove1059 : AIMoveBase
{
    Vector3? pos = null;
    public AIMove1059(EntityBase entity) : base(entity)
    {
        if (entity.NewAI_Type.Equals("AI_MoveToPos"))
        {
            SurvivorMonsterBehaviour behaviour = entity.GetComponent<SurvivorMonsterBehaviour>();
            if (behaviour != null && behaviour.m_Maker != null)
            {
                int point = behaviour.m_Maker.GetAngleOut();
                pos = behaviour.MonsterPosEdgeScreen(point);
            }
        }
    }
    protected override void OnInitBase()
    {
        this.angle = Utils.getAngle((pos != null ? (Vector3)pos : GameLogic.Self.position) - this.m_Entity.position);
    }

    protected override void OnUpdate()
    {
        MoveNormal();
    }


    private void MoveNormal()
    {
        if (Time.frameCount % 2 == 0)
        {
            this.m_MoveData.angle = this.angle;
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

    private float angle;
}
