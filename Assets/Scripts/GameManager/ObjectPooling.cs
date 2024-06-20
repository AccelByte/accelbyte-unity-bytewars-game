// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling
{
    private const int InitialCapacity = 4;

    private readonly Dictionary<string, List<Component>> pooledObjects = new();
    private readonly Transform container;

    public ObjectPooling(Transform container, GameEntityAbs[] gamePrefabs, FxEntity[] fxPrefabs)
    {
        this.container = container;

        foreach (GameEntityAbs prefab in gamePrefabs)
        {
            InitPooledItems(prefab, InitialCapacity);
        }

        foreach (FxEntity fx in fxPrefabs)
        {
            InitPooledItems(fx, InitialCapacity);
        }
    }

    public T Get<T>(T referencePrefab) where T : Component
    {
        bool shouldInitNewPool = !pooledObjects.TryGetValue(referencePrefab.name, out List<Component> items);
        if (shouldInitNewPool)
        {
            items = InitPooledItems(referencePrefab, InitialCapacity, true);
        }

        foreach (T item in items)
        {
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        T newItem = Object.Instantiate(referencePrefab, container);
        newItem.gameObject.SetActive(true);
        items.Add(newItem);

        return newItem;
    }

    private List<Component> InitPooledItems<T>(T prefab, int count, 
        bool setLastActive = false) where T : Component
    {
        List<Component> items = new List<Component>(count);
        for (int index = 0; index < count; index++)
        {
            T newItem = Object.Instantiate(prefab, container);

            bool lastIndex = index == count - 1;
            newItem.gameObject.SetActive(lastIndex && setLastActive);

            if (newItem is GameEntityAbs gameEntity)
            {
                gameEntity.SetId(index);
            }

            items.Add(newItem);
        }

        pooledObjects[prefab.name] = items;

        return items;
    }

    public void ResetAll()
    {
        foreach (KeyValuePair<string, List<Component>> kvp in pooledObjects)
        {
            foreach (Component item in kvp.Value)
            {
                if (item is GameEntityAbs gameEntity)
                {
                    gameEntity.Reset();
                }

                if (item is FxEntity fxEntity)
                {
                    fxEntity.Reset();
                }

                item.gameObject.SetActive(false);
            }
        }
    }

    public void DestroyAll()
    {
        foreach (KeyValuePair<string, List<Component>> kvp in pooledObjects)
        {
            foreach (Component item in kvp.Value)
            {
                Object.Destroy(item.gameObject);
            }
        }
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<string, List<Component>> kvp in pooledObjects)
        {
            kvp.Value.Clear();
        }
    }
}
