using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion.Transfer.Model
{
    [Serializable]
    public class AttackPawnCommand
    {
        public enum PawnCommand : byte
        {
            Wait_Combat = 0,
            Goto, //идти
            Attack, //стрелять
            AttackMelee, //бить вплотную
            Equip, //взять как оружие
            TakeInventory, //взять
            Wear, //надеть
            DropEquipment, //бросить оружие
            RemoveApparel, //снять одежду
            Ingest, //сьесть
            Strip, //раздеть труп
            TendPatient, //самолечение?
            OC_InventoryDrop, //это не job, а простая команда на дроп из инвентаря
            //Deconstruct, //разобрать стену (не работает без боевого режима)
            //Mine, //добывать скалу (не работает без боевого режима)
            //HarvestDesignated, //срубить дерево (не работает без боевого режима)

            //HaulToCell, //перенести на склад
        }

        public int HostPawnID { get; set; }
        public PawnCommand Command { get; set; }
        public IntVec3S TargetPos { get; set; }
        public int TargetID { get; set; }
        public string TargetDefName { get; set; }

    }
}
