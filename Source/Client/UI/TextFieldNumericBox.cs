using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorldOnlineCity.UI
{
    internal class TextFieldNumericBox : DialogControlBase
    {
        public Func<int> GetValue;
        public Action<int> SetValue;
        public Action<TextFieldNumericBox, int> ValueChanged;
        public Func<bool> Editable = () => true;
        public int Min = 0;
        public int Max = int.MaxValue;
        public bool ShowButton = true;

        private string Buffer;
        private int Value;
        private string ControlName = "TFNB" + (++NameNumLast).ToString();
        private static int NameNumLast = 0;

        public TextFieldNumericBox()
        {
            GetValue = () => Value;
            SetValue = (val) => Value = val;
        }

        public TextFieldNumericBox(Func<int> getValue = null, Action<int> setValue = null, Func<bool> editable = null, Action<TextFieldNumericBox, int> valueChanged = null)
        {
            if (getValue != null) GetValue = getValue; else GetValue = () => Value;
            if (setValue != null) SetValue = setValue; else SetValue = (val) => Value = val;
            if (editable != null) Editable = editable;
            ValueChanged = valueChanged;
        }
        public TextFieldNumericBox(TransferableOneWay item, Func<bool> editable = null, Action<TextFieldNumericBox, int> valueChanged = null)
        {
            if (editable != null) Editable = editable;
            GetValue = () => item.CountToTransfer;
            SetValue = (val) => item.AdjustTo(val);
            ValueChanged = valueChanged;
        }

        public void Drow(Rect inRect)
        {
            int val = GetValue();

            int num2 = GenUI.CurrentAdjustmentMultiplier(); //зажали кнопку для прибавления по 10/100
            //кнопка <
            if (ShowButton)
            {
                Rect rect4 = new Rect(inRect.x, inRect.y, inRect.height, inRect.height).ContractedBy(1f);
                if (val - num2 < Min || !Editable()) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect4, "<", true, false, true))
                {
                    GUI.color = Color.white;
                    if (val - num2 < Min || !Editable()) return;
                    val -= 1 * num2;
                    Buffer = val.ToString();
                    SetValue(val);
                    if (ValueChanged != null) ValueChanged(this, val);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
            }
            GUI.color = Color.white;
            string editBuffer = Buffer ?? Min.ToString();
            int valBuffer = val;
            var rect = ShowButton 
                ? new Rect(inRect.x + inRect.height, inRect.y, inRect.width - inRect.height * 2, inRect.height).ContractedBy(2f)
                : new Rect(inRect.x, inRect.y, inRect.width, inRect.height).ContractedBy(2f);
            //поле ввода
            // { Widgets.TextFieldNumeric<int>(rect, ref valBuffer, ref editBuffer, Min, Max);
            GUI.SetNextControlName(ControlName);
            string text2 = GUI.TextField(rect, editBuffer, Text.CurTextFieldStyle).Trim(); //Widgets.TextField(rect, editBuffer);
            var focus = GUI.GetNameOfFocusedControl() == ControlName;
            var correct = int.TryParse(text2, out int var2);
            if (focus || text2 != editBuffer && correct)
            {
                if (var2 < Min && correct) text2 = (var2 = Min).ToString();
                if (var2 > Max && correct) text2 = (var2 = Max).ToString();
                if (correct || text2.Length == 1 && (char.IsDigit(text2[0]) || text2[0] == '-'))
                {
                    editBuffer = text2;
                }
                if (correct) valBuffer = var2;
            }
            else if (valBuffer.ToString() != editBuffer) editBuffer = valBuffer.ToString();
            // }
            if (valBuffer != val)
            {
                SetValue(val = valBuffer);
                if (ValueChanged != null) ValueChanged(this, val);
            }
            Buffer = editBuffer;
            //кнопка >
            if (ShowButton)
            {
                Rect rect4 = new Rect(inRect.xMax - inRect.height, inRect.y, inRect.height, inRect.height).ContractedBy(1f);
                if (val + num2 > Max || !Editable()) GUI.color = Color.gray;
                if (Widgets.ButtonText(rect4, ">", true, false, true))
                {
                    GUI.color = Color.white;
                    if (val + num2 > Max || !Editable()) return;
                    val += 1 * num2;
                    Buffer = val.ToString();
                    SetValue(val);
                    if (ValueChanged != null) ValueChanged(this, val);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
            }
            GUI.color = Color.white;
        }
    }
}
