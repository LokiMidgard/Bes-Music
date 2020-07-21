
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace MusicPlayer.Triggers
{
    internal class EqualsTrigger : StateTriggerBase
    {



        public object Input
        {
            get { return (object)this.GetValue(InputProperty); }
            set { this.SetValue(InputProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Input.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputProperty =
            DependencyProperty.Register("Input", typeof(object), typeof(EqualsTrigger), new PropertyMetadata(null, ProcessChange));



        public object Reference
        {
            get { return (object)this.GetValue(ReferenceProperty); }
            set { this.SetValue(ReferenceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Reference.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReferenceProperty =
            DependencyProperty.Register("Reference", typeof(object), typeof(EqualsTrigger), new PropertyMetadata(null, ProcessChange));



        public bool TriggerOnEquals
        {
            get { return (bool)this.GetValue(TriggerOnEqualsProperty); }
            set { this.SetValue(TriggerOnEqualsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TriggerOnEquals.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TriggerOnEqualsProperty =
            DependencyProperty.Register("TriggerOnEquals", typeof(bool), typeof(EqualsTrigger), new PropertyMetadata(true, ProcessChange));

        private static void ProcessChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = d as EqualsTrigger;
            me.UpdateTrigger();
        }

        private void UpdateTrigger()
        {
            var input = this.Input;
            var reference = this.Reference;

            if (reference is string refString)
            {
                if (input is int ii && int.TryParse(refString, out var ri))
                {
                    input = ii;
                    reference = ri;
                }
                else if (input is double id && double.TryParse(refString, out var rd))
                {
                    input = id;
                    reference = rd;
                }
                else if (input is long il && double.TryParse(refString, out var rl))
                {
                    input = il;
                    reference = rl;
                }
                else if (input is float @if && float.TryParse(refString, out var rf))
                {
                    input = @if;
                    reference = rf;
                }
                else if (input is short @is && short.TryParse(refString, out var rs))
                {
                    input = @is;
                    reference = rs;
                }
                else if (input is byte ib && short.TryParse(refString, out var rb))
                {
                    input = ib;
                    reference = rb;
                }
                else if (input is bool ib2 && bool.TryParse(refString, out var rb2))
                {
                    input = ib2;
                    reference = rb2;
                }
            }

            var active = Equals(input, reference);
            if (!this.TriggerOnEquals)
                active = !active;
            this.SetActive(active);
        }

    }

}
