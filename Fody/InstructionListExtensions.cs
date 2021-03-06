using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

public static class InstructionListExtensions
{
    public static void Prepend(this Collection<Instruction> collection, ICollection<Instruction> instructions)
    {
        var index = 0;
        foreach (var instruction in instructions)
        {
            collection.Insert(index, instruction);
            index++;
        }
    }

    /// <summary>
    /// Inserts a set of instructions at a given <paramref name="exitPointInstructionIndex"/>, ensuring that all branches that previously
    /// pointed to the return statement now point to the new instructions.
    /// 
    /// It's important that this method is only called for RET instruction exit points, otherwise we'd also need to fix
    /// up ExceptionHandler delimiter points.
    /// </summary>
    public static void InsertAtMethodReturnPoint(this MethodBody methodBody, int exitPointInstructionIndex, ICollection<Instruction> instructions)
    {
        var exitPointInstruction = methodBody.Instructions[exitPointInstructionIndex];

        var targetUpdateActions = GetMethodReturnPointReferenceTargetUpdateActions(methodBody.Instructions, exitPointInstruction).ToList();

        foreach (var instruction in instructions.Reverse())
        {
            methodBody.Instructions.Insert(exitPointInstructionIndex, instruction);
        }

        var newTargetInstruction = instructions.First();
        foreach (var targetUpdateAction in targetUpdateActions)
        {
            targetUpdateAction(newTargetInstruction);
        }
    }

    private static IEnumerable<Action<Instruction>> GetMethodReturnPointReferenceTargetUpdateActions(
        Collection<Instruction> instructions,
        Instruction exitPointInstruction)
    {
        foreach (var instruction in instructions)
        {
            if (exitPointInstruction.Equals(instruction.Operand))
                yield return x => instruction.Operand = x;

            // For switch instructions, operand is an Instruction-Array:
            var operandInstructions = instruction.Operand as Instruction[];
            if (operandInstructions != null)
            {
                for (var i = 0; i < operandInstructions.Length; i++)
                {
                    if (exitPointInstruction.Equals(operandInstructions[i]))
                    {
                        var localI = i;
                        yield return x => operandInstructions[localI] = x;
                    }
                }
            }
        }
    }
}