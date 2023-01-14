using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;

using Int8 = System.SByte;

    /**
    * A class storing a Connect 4 position.
    * Functions are relative to the current player to play.
    *
    * A binary bitboard representation is used.
    * Each column is encoded on HEIGHT + 1 bits.
    *
    * bit order to encode for a 7x6 board:
    * .  .  .  .  .  .  .
    * 5 12 19 26 33 40 47
    * 4 11 18 25 32 39 46
    * 3 10 17 24 31 38 45
    * 2  9 16 23 30 37 44
    * 1  8 15 22 29 36 43
    * 0  7 14 21 28 35 42
    *
    * Position is stored as
    * - a bitboard "mask" with 1 on any color stones
    * - a bitboard "position" with 1 on stones of current player
    *
    * current player bitboard can be transformed into a key by adding an extra
    * bit on top of the last non empty cell of each column.
    * This allow to identify all the empty cells whithout needing "mask" bitboard
    *
    * current player "x" = 1, opponent "o" = 0
    * board     position  mask      key       bottom
    *           0000000   0000000   0000000   0000000
    * .......   0000000   0000000   0001000   0000000
    * ...o...   0000000   0001000   0010000   0000000
    * ..xx...   0011000   0011000   0011000   0000000
    * ..ox...   0001000   0011000   0001100   0000000
    * ..oox..   0000100   0011100   0000110   0000000
    * ..oxxo.   0001100   0011110   1101101   1111111
    *
    * current player "o" = 1, opponent "x" = 0
    * board     position  mask      key       bottom
    *           0000000   0000000   0001000   0000000
    * ...x...   0000000   0001000   0000000   0000000
    * ...o...   0001000   0001000   0011000   0000000
    * ..xx...   0000000   0011000   0000000   0000000
    * ..ox...   0010000   0011000   0010100   0000000
    * ..oox..   0011000   0011100   0011010   0000000
    * ..oxxo.   0010010   0011110   1110011   1111111
    *
    * key is an unique representation of a board key = position + mask + bottom
    */

public class Node
{
    public enum State
    {
        Empty,
        Player1,
        Player2
    }

    public enum DebugState
    {
        Leaf,
        Transposed,
        WinNext,
        LooseNext,
        Normal,
        BetaPruned
    }

    public DebugState debugState;
    public int a;
    public int b;

    //Static Datas
    public const int WIDTH = 7;
    public const int HEIGHT = 6;

    //Node Data
    public bool isP1Turn = true;
    public Int8 remainingStones = 42;
    public int lastPlay = -1;

    //Tree values
    public List<Node> children = new List<Node>();
    public Int8 value = -99;

    // Bitmaps
    public UInt64 position;
    public UInt64 mask;

    //Static Bitmaps
    private static UInt64 Bottom(int _width, int _height)
    {
        return _width == 0 ? 0 : Bottom(_width - 1, _height) | (UInt64)1 << (_width - 1) * (_height + 1);
    }
    private static UInt64[] VerticalMasks(int _width, int _height)
    {
        UInt64[] result = new UInt64[_width];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                result[x] |= Mask(x, y);
            }
        }
        return result;
    }
    static readonly UInt64 bottomMask = Bottom(WIDTH, HEIGHT);
    static readonly UInt64 boardMask = bottomMask * ((1 << HEIGHT)-1);
    public static readonly UInt64[] verticalMask = VerticalMasks(WIDTH,HEIGHT);
    
    //Constructors
    public Node()
    {
        position = 0;
        mask = 0;
    }
    public Node(Node _node)
    {
        position = _node.position;
        mask = _node.mask;
        isP1Turn = _node.isP1Turn;
        remainingStones = _node.remainingStones;
    }

    //Debug
    public override string ToString()
    {
        string result = $"V={value},A={a},B={b}, P1? {isP1Turn}, {remainingStones} stones, {children.Count} children, {debugState}:\n";
        for (int y = 5; y >= 0; y--)
        {
            result += "|";
            for (int x = 0; x < 7; x++)
            {
                switch (GetState(x, y))
                {
                    case State.Empty:
                        result += "` `|";
                        break;
                    case State.Player1:
                        result += " 0 |";
                        break;
                    case State.Player2:
                        result += " X |";
                        break;
                }
            }
            result += "\n";
        }

        return result;
    }
    public static string DebugBitmap(UInt64 _bitmap)
    {
        string result = "";
        for (int y = 6; y >= 0; y--)
        {
            result += "|";
            for (int x = 0; x < 7; x++)
            {
                UInt64 m = Mask(x, y);
                if ((m & _bitmap) != 0)
                {
                    result += " 1 |";
                }
                else
                {
                    result += " 0 |";
                }
            }
            result += "\n";
        }

        return result;
    }

    
    /// <summary>
    /// Return the player who have a stone in a given cell
    /// </summary>
    /// <param name="_x"></param>
    /// <param name="_y"></param>
    /// <returns></returns>
    public State GetState(int _x, int _y)
    {
        UInt64 m = Mask(_x, _y);
        if ((mask & m) == 0)
        {
            return State.Empty;
        }
        if ((position & m) != 0)
        {
            return isP1Turn ? State.Player1 : State.Player2;
        }
        return isP1Turn ? State.Player2 : State.Player1;
    }
    
    /// <summary>
    /// Return the Winner of the game at this state, Empty id there is none
    /// </summary>
    /// <returns></returns>
    public State GetWinner()
    {
        if (IsAligned(position)) return isP1Turn ? State.Player1 : State.Player2;
        if (IsAligned(position ^ mask)) return isP1Turn ? State.Player2 : State.Player1;
        return State.Empty;
    }
    
    /// <summary>
    /// Return true is there is 4 aligned stone in any direction for a player stones position
    /// </summary>
    /// <param name="_pos">A bitmap of the positions of a player's stones</param>
    /// <returns></returns>
    public bool IsAligned(UInt64 _pos)
    {
        // horizontal 
        UInt64 m = _pos & (_pos >> (HEIGHT + 1));
        if ((m & (m >> (2 * (HEIGHT + 1)))) != 0) return true;

        // diagonal 1
        m = _pos & (_pos >> HEIGHT);
        if ((m & (m >> (2 * HEIGHT))) != 0) return true;

        // diagonal 2 
        m = _pos & (_pos >> (HEIGHT + 2));
        if ((m & (m >> (2 * (HEIGHT + 2)))) != 0) return true;

        // vertical;
        m = _pos & (_pos >> 1);
        if ((m & (m >> 2)) != 0) return true;

        return false;
    }

    /// <summary>
    /// Check if a player can be play in a given column
    /// </summary>
    /// <param name="_x">The column id</param>
    /// <returns></returns>
    public bool CanPlayIn(int _x)
    {
        //Check if the upper bit of the mask bitmap in column x is empty
        return (mask & TopMask(_x)) == 0;
    }

    /// <summary>
    /// Return a bitmap mask coresponding to the upper bit of a column
    /// </summary>
    /// <param name="_x">The column id</param>
    /// <returns></returns>
    public static UInt64 TopMask(int _x)
    {
        return ((UInt64) 1 << (HEIGHT - 1)) << _x * (HEIGHT + 1);
    }
    /// <summary>
    /// Return a bitmap mask coresponding to the Bottom bit of a column
    /// </summary>
    /// <param name="_x">The column id</param>
    /// <returns></returns>
    public static UInt64 BottomMask(int _x)
    {
        return (UInt64)1 << _x * (HEIGHT + 1);
    }
    /// <summary>
    /// Return a bitmask mask coresponding to the coordinate's bit
    /// </summary>
    /// <returns></returns>
    public static UInt64 Mask(int _x, int _y)
    {
        return ((UInt64)1 << _y) << _x * (HEIGHT + 1);
    }

    /// <summary>
    /// Update the node's state from a player's play in a given column
    /// </summary>
    /// <param name="_x">The column in wich the player have played</param>
    public void Play(int _x)
    {
        position ^= mask;
        mask |= mask + BottomMask(_x);
        lastPlay = _x;

        //Datas
        remainingStones--;

        isP1Turn = !isP1Turn;
    }

    /// <summary>
    /// Computre the value of the node based on the state of the game.
    /// </summary>
    /// <returns>The value of the node</returns>
    public int Evaluate() 
    {
        State result = GetWinner();
        if (result == State.Player1)
        {
            value = WinningValue();
        }
        else if (result == State.Empty)
        {
            value = 0;
        }
        else
        {
            value = (Int8)(-WinningValue());
        }
        return value;
    }

    /// <summary>
    /// The value the evaluation of the node will return is the player is winning
    /// </summary>
    /// <param name="_stoneModifier">A modifier to the number a current stone</param>
    /// <returns>The value of the node</returns>
    public Int8 WinningValue(int _stoneModifier = 0)
    {
        return (Int8)((remainingStones + _stoneModifier) / 2 + 1);
    }

    /// <summary>
    /// Return a key coresponding to the current state of the board
    /// </summary>
    /// <returns></returns>
    public UInt64 Key()
    {
        return position + mask;
    }


    /// <summary>
    /// Return a bitmask of the possible winning positions for the opponent
    /// </summary>
    /// <returns></returns>
    public UInt64 OpponentWinningPosition() {
        return ComputeWinningPosition(position ^ mask, mask);
    }
    /// <summary>
    /// Return a bitmask of the possible positions for the curent player
    /// </summary>
    /// <returns></returns>
    public UInt64 Possible() {
        return (mask + bottomMask) & boardMask;
    }
    /// <summary>
    /// Return a bitmask of the winning positions for the current player
    /// </summary>
    /// <returns></returns>
    public static UInt64 ComputeWinningPosition(UInt64 _position, UInt64 _mask)
    {
        // vertical;
        UInt64 r = (_position << 1) & (_position << 2) & (_position << 3);

        //horizontal
        UInt64 p = (_position << (HEIGHT + 1)) & (_position << 2 * (HEIGHT + 1));
        r |= p & (_position << 3 * (HEIGHT + 1));
        r |= p & (_position >> (HEIGHT + 1));
        p >>= 3 * (HEIGHT + 1);
        r |= p & (_position << (HEIGHT + 1));
        r |= p & (_position >> 3 * (HEIGHT + 1));

        //diagonal 1
        p = (_position << HEIGHT) & (_position << 2 * HEIGHT);
        r |= p & (_position << 3 * HEIGHT);
        r |= p & (_position >> HEIGHT);
        p >>= 3 * HEIGHT;
        r |= p & (_position << HEIGHT);
        r |= p & (_position >> 3 * HEIGHT);

        //diagonal 2
        p = (_position << (HEIGHT + 2)) & (_position << 2 * (HEIGHT + 2));
        r |= p & (_position << 3 * (HEIGHT + 2));
        r |= p & (_position >> (HEIGHT + 2));
        p >>= 3 * (HEIGHT + 2);
        r |= p & (_position << (HEIGHT + 2));
        r |= p & (_position >> 3 * (HEIGHT + 2));

        return r & (boardMask ^ _mask);
    }
    /// <summary>
    /// Return a bitmask of the possible non losing positions for the current player
    /// </summary>
    /// <returns></returns>
    public UInt64 PossibleNonLosingPositions() {
        UInt64 possibleMask = Possible();
        UInt64 opponentWin = OpponentWinningPosition();
        UInt64 forcedMoves = possibleMask & opponentWin;
        if(forcedMoves != 0) {
            if((forcedMoves & (forcedMoves - 1)) != 0)  // check if there is more than one forced move
                return 0;                               // the opponnent has two winning moves and you cannot stop him
            else possibleMask = forcedMoves; // enforce to play the single forced move
        }
        return possibleMask & ~(opponentWin >> 1); // avoid to play below an opponent winning spot
    }
    /// <summary>
    /// Return a bitmask of the possible winning positions for the current player
    /// </summary>
    /// <returns></returns>
    public UInt64 WinningPositions() 
    {
        return ComputeWinningPosition(position, mask) & Possible();
    }

    /// <summary>
    /// COnvert a bitmask of postion into a list of column id
    /// </summary>
    /// <param name="_bitmap">The bitmap containing the positions</param>
    /// <param name="_order">The order in wich the move will be sorted</param>
    /// <returns>A list of columns id</returns>
    public static List<int> BitmapToMoves(UInt64 _bitmap, int[] _order)
    {
        if (_bitmap == 0) return null;

        List<int> result = new List<int>();
        for (int i = 0; i < WIDTH; i++)
        {
            int x = _order[i];
            if ((verticalMask[x] & _bitmap) != 0)
            {
                result.Add(x);
            }
        }
        return result;
    }
    /// <summary>
    /// Return the score of a move based on the number of winnig position
    /// </summary>
    /// <param name="_move">The move to evaluate</param>
    /// <returns>The score of the move</returns>
    public uint MoveScore(UInt64 _move) {
        return PopCount(ComputeWinningPosition(position | _move, mask));
    }

    /// <summary>
    /// Counts number of bit set to one in a 64bits integer
    /// </summary>
    public static uint PopCount(UInt64 _m)
    {
        uint c = 0;
        for (c = 0; _m != 0; c++) _m &= _m - 1;
        return c;
    }
}
