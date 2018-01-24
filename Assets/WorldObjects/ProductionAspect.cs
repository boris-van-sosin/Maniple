using Maniple;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProductionAspect
{
    public ProductionAspect(Building attachedBuilding, Vector3 spawnPoint, Quaternion spawnRotation, Vector3 rallyPoint)
    {
        _spawnPoint = spawnPoint;
        _rallyPoint = rallyPoint;
        _spawnRotation = spawnRotation;
        _attachedBuilding = attachedBuilding;
    }

    public void EnqueueProduction(ProductionItem item)
    {
        _buildQueue.Enqueue(item);
    }

    public void BuildQueueStep()
    {
        if (_buildQueue.Count > 0)
        {
            ProductionItem item = _buildQueue.Peek();
            if (!_startedCurrent && _attachedBuilding.Owner.GetResource(GameResources.ResourceType.Money) >= item.Cost)
            {
                _startedCurrent = true;
                _productionStarted = Time.unscaledTime;
                _attachedBuilding.Owner.AddResource(GameResources.ResourceType.Money, -item.Cost);
            }
            if (_startedCurrent && ((Time.unscaledTime - _productionStarted) >= item.ProductionTime))
            {
                if (_attachedBuilding != null && _attachedBuilding.Owner != null)
                {
                    Debug.Log(string.Format("Finished producing {0}", _buildQueue.Peek()));
                    ProduceItem(_buildQueue.Dequeue());
                }
                else
                {
                    EmptyQueue();
                }
                _startedCurrent = false;
            }
        }
    }

    private void ProduceItem(ProductionItem item)
    {
        switch (item.ItemType)
        {
            case ProductionItemType.ProductionItemCompany:
                CompanyObject newCompany = _attachedBuilding.Owner.AddCompany(item.ProductionKey, _rallyPoint, _spawnRotation);
                Unit firstUnit = _attachedBuilding.Owner.AddUnit(newCompany.CommanderKey, _spawnPoint, _spawnRotation);
                Formation f = newCompany.GetComponentInChildren<Formation>();
                f.Forming = true;
                _attachedBuilding.ProductionTargetFormation = f;
                firstUnit.ContainingFormation = f;
                f.AddUnit(firstUnit);
                ProductionItem subItem = new ProductionItem()
                {
                    ItemType = ProductionItemType.ProductionItemUnit,
                    Cost = item.Cost,
                    ProductionTime = item.ProductionTime,
                    ProductionKey = newCompany.UnitKey,
                    TargetFormation = f
                };
                for (int i = 0; i < f.MaxUnits - 1; ++i)
                {
                    EnqueueProduction(subItem);
                }
                EnqueueProduction(ProductionItem.FinishForming(f));
                OnFormationCreate(f);
                break;
            case ProductionItemType.ProductionItemUnit:
                if (item.TargetFormation != null)
                {
                    Unit currUnit = _attachedBuilding.Owner.AddUnit(item.ProductionKey, _spawnPoint, _spawnRotation);
                    currUnit.ContainingFormation = item.TargetFormation;
                    item.TargetFormation.AddUnit(currUnit);
                }
                break;
            case ProductionItemType.ProductionItemFinalizeFormation:
                if (item.TargetFormation != null)
                {
                    item.TargetFormation.FinishForming();
                    OnFormationFinalize(item.TargetFormation);
                }
                break;
            default:
                break;
        }
    }

    private void EmptyQueue()
    {
        _buildQueue.Clear();
    }

    public bool IsProducing
    {
        get
        {
            return _buildQueue.Count > 0;
        }
    }

    private readonly Building _attachedBuilding;
    private Queue<ProductionItem> _buildQueue = new Queue<ProductionItem>();
    private Vector3 _spawnPoint, _rallyPoint;
    private Quaternion _spawnRotation;
    private float _productionStarted;
    private bool _startedCurrent;

    public struct ProductionItem
    {
        public ProductionItemType ItemType { get; set; }
        public string ProductionKey { get; set; }
        public float ProductionTime { get; set; }
        public int Cost { get; set; }
        public Formation TargetFormation { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", ItemType, ProductionKey);
        }
        
        public static ProductionItem FinishForming(Formation f)
        {
            return new ProductionItem()
            {
                ItemType = ProductionItemType.ProductionItemFinalizeFormation,
                Cost = 0,
                ProductionKey = "",
                ProductionTime = 0.0f,
                TargetFormation = f
            };
        }
    }

    public enum ProductionItemType { ProductionItemCompany, ProductionItemUnit, ProductionItemFinalizeFormation };

    public delegate void FormationDlg(Formation f);
    public event FormationDlg OnFormationCreate;
    public event FormationDlg OnFormationFinalize;
}
