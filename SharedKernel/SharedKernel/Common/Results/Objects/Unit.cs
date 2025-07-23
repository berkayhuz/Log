namespace SharedKernel.Common.Results.Objects;
public readonly struct Unit
{
    public static readonly Unit Value = new();
    public override string ToString() => "()";
}
