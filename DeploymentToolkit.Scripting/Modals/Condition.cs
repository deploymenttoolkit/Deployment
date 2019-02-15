namespace DeploymentToolkit.Scripting.Modals
{
    public enum CompareType
    {
        String,
        Number
    }

    public class Condition
    {
        public CompareType CompareType { get; set; } = CompareType.String;
        public string FirstString { get; set; }
        public string SecondString { get; set; }
        public string Operator { get; set; }

        public bool IsTrue()
        {
            switch(Operator)
            {
                case "==":
                case "!=":
                    {
                        var isNotEqual = Operator == "!=";
                        var result = false;

                        if (CompareType == CompareType.String)
                        {
                            result = FirstString == SecondString;
                        }
                        else
                        {
                            if (!int.TryParse(FirstString, out var firstNumber) || !int.TryParse(SecondString, out var secondNumber))
                                return false;

                            result = firstNumber == secondNumber;
                        }

                        if (isNotEqual)
                            return !result;
                        return result;
                    }

                case ">":
                case ">=":
                case "<":
                case "<=":
                    {
                        if (!int.TryParse(FirstString, out var firstNumber) || !int.TryParse(SecondString, out var secondNumber))
                            return false;

                        switch(Operator)
                        {
                            case ">":
                                return firstNumber > secondNumber;
                            case ">=":
                                return firstNumber >= secondNumber;
                            case "<":
                                return firstNumber < secondNumber;
                            case "<=":
                                return firstNumber <= secondNumber;
                        }
                        
                        // This should never happen but the compiler wants it
                        return false;
                    }

                default:
                    {
                        // This should never happen
                        System.Diagnostics.Debug.WriteLine($"Invalid operator {Operator}");
                    }
                    return false;
            }
        }
    }
}
