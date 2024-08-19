using System.Collections;
using System.Collections.Generic;
using TableTool;
using UnityEngine;


public class Buff_redAndBlue : LocalBean
{
    public int BuffID { get; private set; }

    public int AvailableLocation { get; private set; }

    public int Weights { get; private set; }

    public int Type { get; private set; }

    public int price { get; private set; }

    public int[] Connect { get; private set; }

    public int EffectEnumeration { get; private set; }

    public string[] Value { get; private set; }

    public int EffectType { get; private set; }

    public int EffectTypeValue { get; private set; }

    public int Repeatable { get; private set; }

    public int[] EffectiveUnit { get; private set; }

    public string icon { get; private set; }

    public string Text_TID { get; private set; }

    public string Name_TID { get; private set; }
    protected override bool ReadImpl()
    {
        this.BuffID = base.readInt();
        this.AvailableLocation = base.readInt();
        this.Weights = base.readInt();
        this.Type = base.readInt();
        this.price = base.readInt();
        this.Connect = base.readArrayint();
        this.EffectEnumeration = base.readInt();
        this.Value = base.readArraystring();
        this.EffectType = base.readInt();
        this.EffectTypeValue = base.readInt();
        this.Repeatable = base.readInt();
        this.EffectiveUnit = base.readArrayint();
        this.icon = base.readLocalString();
        this.Text_TID = base.readLocalString();
        this.Name_TID = base.readLocalString();
        return true;
    }

    public bool IsEvoBuff()
    {
        return Connect.Length > 0;
    }

}


public class Buff_redAndBlueModle : SingleLocalModel<Buff_redAndBlue, int, Buff_redAndBlueModle>
{
    protected override string SFilename => "Buff_redAndBlue";
    protected override int SGetBeanKey(Buff_redAndBlue bean)
    {
        return bean.BuffID;
    }
    Dictionary<int, int> dic_id2evoid = null;

    protected override void OnInit()
    {
        base.OnInit();
        dic_id2evoid = new Dictionary<int, int>();
        var allitems = GetAllBeans();
        foreach (var item in allitems)
        {
            if (item.Connect.Length > 0)
            {
                foreach (var cid in item.Connect)
                {
                    dic_id2evoid[cid] = item.BuffID;
                }
            }
        }
    }
    public bool IsEvoBuff(int id)
    {
        var buff = GetBeanById(id);
        return buff.IsEvoBuff();
    }
    public int GetEvoBuffId_Id(int id)
    {
        int evoid = 0;
        dic_id2evoid.TryGetValue(id, out evoid);
        return evoid;
    }
}
