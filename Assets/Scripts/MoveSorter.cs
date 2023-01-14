using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Int8 = System.SByte;
static class MoveSorter
{
    /// <summary>
    /// Add a move in the container with its score.
    /// You cannot add more than Node.WIDTH moves
    /// </summary>
    /// <param name="_move"></param>
    /// <param name="_score"></param>
    public static void Add(int _move, uint _score)
    {
        uint pos = size;
        size++;
        for (; pos != 0 && entries[pos - 1].score >= _score; --pos)
        {
            entries[pos] = entries[pos - 1];
        }
        entries[pos].move = _move;
        entries[pos].score = _score;
    }

    /// <summary>
    /// Get next move
    /// </summary>
    /// <returns>Next remaining move with max score and remove it from the container. If no more move is available return 0</returns>
    public static int GetNext()
    {
        if (size != 0)
            return entries[--size].move;
        else
            return -1;
    }

    /// <summary>
    /// Reset the sorter
    /// </summary>
    public static void Reset()
    {
        size = 0;
    }

    // number of stored moves
    private static uint size = 0;

    // Contains size moves with their score ordered by score
    private struct Entry
    {
        public int move;
        public uint score;
    }
    private static readonly Entry[] entries = new Entry[Node.WIDTH];
};