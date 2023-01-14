using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Int8 = System.SByte;

public class ConnectFour : MonoBehaviour
{
    public enum PlayerType
    {
        Player,
        AI
    }

    public enum GameState
    {
        CanPlay,
        WaitDelay,
        AIComputing
    }

    [SerializeField] public int AI_Depth = 6;
    [SerializeField] private bool betterOrdering = true;
    [SerializeField] private int betterOrderingMinDepth = 3;
    [SerializeField] private bool debug = false;
    [SerializeField] private int nodePerFrame = 100; // Amount of node per frame
    [SerializeField] public PlayerType player1;
    [SerializeField] public PlayerType player2;

    //State
    public GameState state;

    //AI
    private Node root;
    private TranspositionTable table;
    private int[] columnOrder;
    private int nbNode = 0;

    //Debug values
    private int nbLeaf = 0;
    private int nbTransposed = 0;
    private int turn = 0;

    public bool curPlay1 = true;
    [HideInInspector] public Node.State win;
    
    //Times
    private int nbFrame = 0;
    [HideInInspector] public float lastTime = 0;
    [HideInInspector] public float deltaTime = 0;
    [HideInInspector] public float[] maxTime = new float[2];
    [SerializeField] public float delayBetweenMoves = 1f;
    private float sTime = 0;

    public Node CurrentNode = null;

    //Events
    public delegate void VoidEvent();
    public delegate void IntEvent(int _i);
    public VoidEvent OnBoardUpdate;
    public VoidEvent OnTimeUpdate;
    public VoidEvent OnReset;
    public IntEvent OnPlay;
    public static ConnectFour Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Rules.Instance != null)
        {
            Rules.Instance.ApplyRules(this);
        }

        root = new Node();
        CurrentNode = root;

        Time.maximumDeltaTime = float.MaxValue;

        maxTime[0] = 0;
        maxTime[1] = 0;

        //Compute central first (in this order : 3,4,2,5,1,6,0)
        columnOrder = new int[Node.WIDTH];
        for (int i = 0; i < columnOrder.Length; i++)
        {
            columnOrder[i] = columnOrder.Length / 2 + (i + 1) / 2 * (-1 + 2 * (i % 2));
        }

        table = new TranspositionTable();
        lastTime = Time.time;
        state = GameState.CanPlay;
        win = Node.State.Empty;
    }

    private void Update()
    {
        if (debug && !Input.GetKeyDown(KeyCode.Space)) return;

        if (win == Node.State.Empty && CurrentNode == root && state != GameState.AIComputing && (curPlay1 ? player1 : player2) == PlayerType.AI)
        {
            StartCoroutine(AI_Play(curPlay1 ? Node.State.Player1 : Node.State.Player2));
        }

        if (state == GameState.WaitDelay && Time.time - sTime > delayBetweenMoves)
        {
            EndTurn();
        }
    }

    IEnumerator AI_Play(Node.State _aiId)
    {
        Debug.Log("AI start computing");
        state = GameState.AIComputing;

        root.children.Clear();
        table.Reset();
        nbNode = 0;
        nbLeaf = 0;
        nbTransposed = 0;

        int value = 0;
        IEnumerator enumerator = NegaMax(root, AI_Depth, -int.MaxValue, int.MaxValue, curPlay1 ? 1 : -1); //Can't use int.MinValue because strangely : -int.MinValue = int.MinValue
        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null) value = (Int8)enumerator.Current;
            if (nbNode % nodePerFrame == 0) yield return null;
        }

        Debug.Log($"Value : {value} : Generated : {nbLeaf} leafs for {nbNode} nodes ({nbTransposed} transposed)");

        if (debug)
        {
            Debug.Log($"Player{(_aiId == Node.State.Player1 ? '1' : '2')}'s Turn.");
            DebugMinMax(root, AI_Depth);
        }

        //first node with value
        Node next = root.children.First(child => child.value == value);

        Debug.Log("AI end computing");

        while (CurrentNode != root)
        {
            yield return null;
        }

        root = next;
        state = GameState.WaitDelay;
    }

    /// <summary>
    /// Compute the value of a position based on the value of the possibles next move.
    /// NegaMax is an alternaive of MiniMax algorith where Min and Max use the same evaluation methode.
    /// The value of a node is equal to negative best value of the childs 
    /// </summary>
    /// <param name="_node">The original state of the game</param>
    /// <param name="_depth">The maximum number of move the AI will check</param>
    /// <param name="_alpha">The minimum value currently computed</param>
    /// <param name="_beta">The maximun value currently computed</param>
    /// <param name="_color">The multiplactor of the value player1 : 1, player 2 : -1</param>
    /// <param name="_parentCoroutine"></param>
    /// <returns></returns>
    IEnumerator NegaMax(Node _node, int _depth, int _alpha, int _beta, int _color) //color : (p1 = 1) (p2 = -1)
    {
        _node.a = _alpha;
        _node.b = _beta;
        //if depth == 0 or a winner is declared or the board is full : evaluate the node
        if (_depth == 0 || _node.GetWinner() != Node.State.Empty || _node.remainingStones == 0)
        {
            nbLeaf++;
            _node.debugState = Node.DebugState.Leaf;
            _node.value = (Int8)(_color * _node.Evaluate());
            yield return (Int8)(-_node.value);
            yield break;
        }

        //If already computed : Get the value from the transposition table and return it
        _node.value = table.Get(_node.Key());
        if (_node.value != Int8.MinValue)
        {
            nbTransposed++;
            _node.debugState = Node.DebugState.Transposed;
            yield return (Int8)(-_node.value);
            yield break;
        }

        //If can win on the next move, only compute winning child
        List<int> winning = Node.BitmapToMoves(_node.WinningPositions(), columnOrder);
        if (winning != null)
        {
            //Create the winning child
            Node newChild = new Node(_node);
            newChild.Play(winning[0]);
            _node.children.Add(newChild);
            nbNode++;

            //Evaluate the child
            IEnumerator enumerator = NegaMax(newChild, _depth - 1, -_beta, -_alpha, -_color);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null) _node.value = (Int8)enumerator.Current;
                if (nbNode % nodePerFrame == 0) yield return null;
            }

            table.Put(_node.Key(), _node.value);
            _node.debugState = Node.DebugState.WinNext;
            yield return (Int8)(-_node.value);
            yield break;
        }

        //If it's impossible not to lose on next move, only compute first losing child
        UInt64 nonLosingPos = _node.PossibleNonLosingPositions();
        List<int> moves = Node.BitmapToMoves(nonLosingPos, columnOrder);
        if (moves == null)
        {
            //Get all possibles move
            List<int> possibles = Node.BitmapToMoves(_node.Possible(), columnOrder);

            //Create the first possible child
            Node newChild = new Node(_node);
            newChild.Play(possibles[0]);
            _node.children.Add(newChild);
            nbNode++;

            //Evaluate the child
            IEnumerator enumerator = NegaMax(newChild, _depth - 1, -_beta, -_alpha, -_color);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null) _node.value = (Int8)enumerator.Current;
                if (nbNode % nodePerFrame == 0) yield return null;
            }

            table.Put(_node.Key(), _node.value);
            _node.debugState = Node.DebugState.LooseNext;
            yield return (Int8)(-_node.value);
            yield break;
        }

        //order the moves to check those with higher chance of winning first
        if (betterOrdering && _depth > betterOrderingMinDepth)
        {
            //Put all the moves in the move sorter
            MoveSorter.Reset();
            for (int i = 0; i < Node.WIDTH; i++)
            {
                int x = columnOrder[i];
                UInt64 move = nonLosingPos & Node.verticalMask[x];
                if (move != 0)
                {
                    MoveSorter.Add(x, _node.MoveScore(move));
                }
            }

            //Get all the move sorted
            moves = new List<int>();
            int next;
            while ((next = MoveSorter.GetNext()) != -1)
            {
                moves.Add(next);
            }
        }

        //compute all non losing childrens
        foreach (var x in moves)
        {
            Node newChild = new Node(_node);
            newChild.Play(x);
            _node.children.Add(newChild);
            nbNode++;

            Int8 v = -Int8.MaxValue;
            IEnumerator enumerator = NegaMax(newChild, _depth - 1, -_beta, -_alpha, -_color);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null) v = (Int8)enumerator.Current;
                if (nbNode % nodePerFrame == 0) yield return null;
            }

            _node.value = Math.Max(_node.value, v);
            _alpha = Math.Max(_alpha, _node.value);

            //prune the branche if better move
            if (_alpha >= _beta)
            {
                _node.debugState = Node.DebugState.BetaPruned;
                _node.value = (Int8)_alpha;
                yield return (Int8)(- _node.value);
                yield break;
            }
        }

        //Stock the current value in the transposition table
        table.Put(_node.Key(), _node.value);
        _node.debugState = Node.DebugState.Normal;
        yield return (Int8)(-_node.value);
        yield break;
    }

    /// <summary>
    /// Debuging Method that print all the nodes computed in the terminal
    /// </summary>
    /// <param name="_node">The parent node</param>
    /// <param name="_depth">The depth that will be printed</param>
    void DebugMinMax(Node _node, int _depth)
    {
        foreach (Node child in _node.children)
        {
            DebugMinMax(child, _depth - 1);
        }

        Debug.Log($"T{turn} Depth {_depth}, {_node}");
    }

    //Call by buttons
    public void PlayerAction(int _index)
    {
        int x = _index % 7;
        if (!root.CanPlayIn(x) || GetCurrentPlayerType() != PlayerType.Player || win != Node.State.Empty || state != GameState.CanPlay) return;

        root.Play(x);
        state = GameState.WaitDelay;
    }

    public PlayerType GetCurrentPlayerType()
    {
        return curPlay1 ? player1 : player2;
    }

    void EndTurn()
    {
        //Check for Winner
        win = root.GetWinner();

        //Update timers
        deltaTime = Time.time - lastTime;
        maxTime[curPlay1 ? 0 : 1] = Mathf.Max(maxTime[curPlay1 ? 0 : 1], deltaTime);
        lastTime = Time.time;
        sTime = Time.time;
        OnTimeUpdate?.Invoke();

        //Change turn
        curPlay1 = !curPlay1;
        turn++;
        
        //Update Game state
        state = GameState.CanPlay;

        //Trigger Event
        CurrentNode = root;
        OnPlay?.Invoke(root.lastPlay);
        OnBoardUpdate?.Invoke();
    }

    public void RestartBtn()
    {
        if (win != Node.State.Empty) StartCoroutine(Restart());
    }

    IEnumerator Restart()
    {
        OnReset?.Invoke();
        yield return new WaitForSeconds(3);
        Start();
        OnBoardUpdate?.Invoke();
    }
}
