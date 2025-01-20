using UnityEngine;

public class PlayerBoundary : MonoBehaviour
{
    private float minX, maxX, minY, maxY;
    
    public void SetBounds(float minXValue, float maxXValue, float minYValue, float maxYValue)
    {
        minX = minXValue + 0.5f;
        maxX = maxXValue - 0.5f;
        minY = minYValue + 0.5f;
        maxY = maxYValue - 0.5f;

        Debug.Log($"PlayerBoundary gesetzt: minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");
    }

    void Update()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}
