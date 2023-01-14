using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using Int8 = System.SByte;


public class TranspositionTable
{
    UInt64 size = (1 << 23) + 9; // size of the transpositin table must be odd, preferably prime number

    private UInt32[] K;
    private Int8[] V;

    public TranspositionTable()
    {
        //Get the Memory before the init of the table
        long a = GC.GetTotalMemory(false);

        K = new UInt32[size];
        V = new Int8[size];
        Reset();

        //Print the size of the table in the terminal
        long b = GC.GetTotalMemory(false);
        Debug.Log($"Transposition table : {(b - a)/1000000} Mo");
    }

    /// <summary>
    /// The index of a key in the table
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    uint Index(UInt64 key) 
    {
        return (uint)(key % size);
    }

    /// <summary>
    /// Reset the table
    /// </summary>
    public void Reset()
    {
        for (UInt64 i = 0; i < size; i++)
        {
            V[i] = Int8.MinValue;
        }
    }

    /// <summary>
    /// Get the value of a key if it has been registered
    /// </summary>
    /// <param name="key"></param>
    /// <returns>Value of the Key if register, Int8.MinValue if not</returns>
    public Int8 Get(UInt64 key)
    {
        uint i = Index(key);  
        if (K[i] == (UInt32)key) // key is possibly trucated
            return V[i];            
        else
            return Int8.MinValue;
    }
    public void Put(UInt64 key, Int8 value)
    {
        uint i = Index(key);
        K[i] = (UInt32)key; // key is possibly trucated
        V[i] = value;
    }


}
