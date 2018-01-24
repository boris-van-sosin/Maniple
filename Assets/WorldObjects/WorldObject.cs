using Maniple;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldObject : MonoBehaviour {
    protected virtual void Awake()
    {
        _statusLines = new string[] { DisplayName };
    }

    protected virtual void Start()
    {
        _unitsLayerMask = LayerMask.GetMask("Units");
        _LOSObstacleLayerMask = LayerMask.GetMask("Default", "Walls");
        _owner = transform.root.GetComponentInChildren<Player>();
        _selectionMarkerRenderer = GetComponent<LineRenderer>();
    }

    protected virtual void Update()
    {
    }

    protected virtual void OnGUI()
    {
    }

    public virtual void SelectThis()
    {
        _selected = true;
        DisplaySelectionMarker();
    }
    public virtual void DeSelectThis()
    {
        _selected = false;
        DisplaySelectionMarker();
    }
    public bool IsSelected { get { return _selected; } }

    public virtual WorldObject GetSelectionObject()
    {
        return this;
    }

    public virtual void MouseClick(ClickHitObject hitObject, Player controller)
    {
    }

    public void IssueOrder(ClickHitObject target, Player controller)
    {
        IssueOrder(target, null, controller);
    }

    public void IssueOrder(ClickHitObject target, ClickHitObject rClickStart, Player controller)
    {
        _orderQueue.Clear();
        InnerIssueOrder(target, rClickStart, controller);
    }

    protected virtual void InnerIssueOrder(ClickHitObject target, ClickHitObject rClickStart, Player controller)
    {
    }

    public void NextOrderFromQueue()
    {
        if (HasQueuedOrders)
        {
            PerformOrderFromQueue(_orderQueue.Peek());
        }
    }

    public virtual void PerformOrderFromQueue(Order o)
    {
        if (o.OrdType == OrderType.Move)
        {
            InnerIssueOrder(o.HitObj, o.RClickStart, _owner);
        }
    }

    public void EnqueueOrder(Order o)
    {
        _orderQueue.Enqueue(o);
        if (_orderQueue.Count == 1)
        {
            PerformOrderFromQueue(o);
        }
    }

    public bool HasQueuedOrders { get { return _orderQueue.Count > 0; } }

    public virtual string[] GetActions()
    {
        return _actions;
    }

    public virtual void PerformAction(string action)
    {
    }

    public virtual void DisplaySelectionMarker()
    {
        if (_selectionMarkerRenderer != null)
        {
            _selectionMarkerRenderer.enabled = _selected;
        }
    }

    public virtual void DisplayOrderMarker(LineRenderer targetLR, ClickHitObject source, ClickHitObject target)
    {
    }

    public virtual IEnumerable<string> StatusLines()
    {
        if (_statusLines == null)
        {
            return null;
        }
        return _statusLines;
    }

    public Player Owner { get { return _owner; } }

    protected Player _owner;
    protected string[] _actions;
    protected bool _selected;
    protected LineRenderer _selectionMarkerRenderer;
    protected int _unitsLayerMask, _LOSObstacleLayerMask;

    public string ProductionName;
    public string DisplayName;

    protected string[] _statusLines = null;

    public Texture2D CardImage;

    protected Queue<Order> _orderQueue = new Queue<Order>();

    public enum OrderType { Move, Reinforce };

    public struct Order
    {
        public OrderType OrdType;
        public ClickHitObject HitObj;
        public ClickHitObject RClickStart;

        public static Order MoveOrder(ClickHitObject hitObj, ClickHitObject rClickStart)
        {
            return new Order()
            {
                OrdType = OrderType.Move,
                HitObj = hitObj,
                RClickStart = rClickStart
            };
        }
        
        public static Order ReinforceOrder()
        {
            return new Order()
            {
                OrdType = OrderType.Reinforce,
                HitObj = null,
                RClickStart = null
            };
        }
    }
}
