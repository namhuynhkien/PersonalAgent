using System.ComponentModel;
using Microsoft.SemanticKernel;

public class MathPlugin
{
    [KernelFunction]
    [Description("Adds two numbers")]
    public static double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2)
    {
        return number1 + number2;
    }

    [KernelFunction]
    [Description("Subtracts two numbers")]
    public static double Subtract(
        [Description("The number to subtract from")] double number1,
        [Description("The number to subtract")] double number2)
    {
        return number1 - number2;
    }
}