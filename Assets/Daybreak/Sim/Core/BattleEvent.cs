namespace Daybreak.Sim
{
    /// <summary>
    /// One entry in the replay log. The client animates these; it never decides outcomes.
    /// Unit instance indices: side A = 0..4, side B = 5..9, in placement order.
    /// </summary>
    public struct BattleEvent
    {
        public int Tick;
        public EventType Type;
        public int Source;
        public int Target;
        public int Value;
        public int Aux;

        public BattleEvent(int tick, EventType type, int source = -1, int target = -1, int value = 0, int aux = 0)
        {
            Tick = tick;
            Type = type;
            Source = source;
            Target = target;
            Value = value;
            Aux = aux;
        }

        public bool Equals(BattleEvent other)
        {
            return Tick == other.Tick && Type == other.Type && Source == other.Source
                && Target == other.Target && Value == other.Value && Aux == other.Aux;
        }
    }
}
