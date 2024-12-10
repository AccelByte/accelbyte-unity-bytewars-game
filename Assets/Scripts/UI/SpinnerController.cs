// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class SpinnerController : MonoBehaviour
{
    [SerializeField, Range(100,900)] private float rotateSpeed = 450f;
    [SerializeField] private Transform[] imageTransforms;

    private void LateUpdate()
    {
        if (!gameObject.activeInHierarchy) 
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0, 0, rotateSpeed * Time.unscaledDeltaTime);
        Vector3 center = transform.position;

        foreach (Transform imageTransform in imageTransforms)
        {
            Vector3 direction = imageTransform.position - center;
            direction = rotation * direction;
            imageTransform.position = center + direction;
        }
    }
}
