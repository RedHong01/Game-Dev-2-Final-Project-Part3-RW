using UnityEngine;
using System.Collections;

public class TileDestroyer : MonoBehaviour
{
    private MapGenerator mapGenerator; // 引用地图生成器
    public float warningDuration = 1f; // 瓦片变色预警持续时间
    public Color warningColor = Color.red; // 瓦片预警颜色
    public float flashSpeed = 4f; // 预警闪烁速度

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found in the scene.");
            return;
        }

        StartCoroutine(DestroyRandomTile());
    }

    IEnumerator DestroyRandomTile()
    {
        while (true)
        {
            Transform randomTile = mapGenerator.GetRandomOpenTile();
            if (randomTile == null)
            {
                Debug.Log("No tiles left to destroy.");
                yield break;
            }

            Renderer tileRenderer = randomTile.GetComponent<Renderer>();
            if (tileRenderer == null)
            {
                Debug.LogError("Selected tile does not have a Renderer.");
                yield break;
            }

            Material tileMaterial = tileRenderer.material;
            Color originalColor = tileMaterial.color;
            float timer = 0f;

            // 瓦片变色预警
            while (timer < warningDuration)
            {
                tileMaterial.color = Color.Lerp(originalColor, warningColor, Mathf.PingPong(Time.time * flashSpeed, 1));
                timer += Time.deltaTime;
                yield return null;
            }

            // 恢复颜色并销毁瓦片
            tileMaterial.color = originalColor;
            Destroy(randomTile.gameObject);

            yield return new WaitForSeconds(1f); // 每次销毁之间的间隔
        }
    }
}