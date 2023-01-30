/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Unity.Collections.LowLevel.Unsafe;
using System;

/// <summary>
/// Represents bit flags and provides convenient methods for manipulating and querying them.
/// </summary>
public struct BitFlags
{
    /// <summary>
    /// Get the integer value representation of the combined flags.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Whether or not the combined flags contains the provided flag.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="flag"></param>
    /// <returns></returns>
    public bool Has<E>(E flag) where E : struct, IConvertible
    {
        var flagsValue = Value;
        var flagValue = UnsafeUtility.EnumToInt(flag);
        return (flagsValue & flagValue) != 0;
    }

    /// <summary>
    /// Add the provided flag to the combined flags.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="flag"></param>
    public void Add<E>(E flag) where E : struct, IConvertible
    {
        var flagsValue = Value;
        var flagValue = UnsafeUtility.EnumToInt(flag);
        Value = (flagsValue | flagValue);
    }

    /// <summary>
    /// Take the provided flag from the combined flags.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="flag"></param>
    public void Take<E>(E flag) where E : struct, IConvertible
    {
        var flagsValue = Value;
        var flagValue = UnsafeUtility.EnumToInt(flag);
        Value = (flagsValue & (~flagValue));
    }

    /// <summary>
    /// Set the integer value of the combined flags.
    /// </summary>
    /// <param name="value"></param>
    public void Set(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Clear the combined flags and reset the integer value to zero.
    /// </summary>
    public void Clear()
    {
        Value = default;
    }
}
