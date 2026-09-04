using System;

[Serializable]
public class FactEffect
{
    public string Fact;
    public bool Value;

    public FactEffect(string fact, bool value)
    {
        Fact = fact;
        Value = value;
    }
}
