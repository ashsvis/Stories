using System;

namespace Stories.Model
{
    [Serializable]
    public partial class EndOperator : BeginOperator
    {
        protected override void TuningControl()
        {
            base.TuningControl();
            Text = "Конец";
        }
    }

}