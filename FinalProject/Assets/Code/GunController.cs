using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GunData
{
    public string gunName; // 枪支名称
    public Gun gunPrefab; // 对应的枪支预制件
}

public class GunController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform weaponHold; // 持枪点
    public List<GunData> gunList; // 可在 Inspector 中调整的枪支列表

    private Gun equippedGun; // 当前装备的枪支

    private void Start()
    {
        // 如果列表中有枪支，默认装备第一个枪支
        if (gunList.Count > 0 && gunList[0].gunPrefab != null)
        {
            EquipGun(gunList[0].gunPrefab);
        }
    }

    public void EquipGun(Gun gunToEquip)
    {
        // 如果已有枪支，销毁它
        if (equippedGun != null)
        {
            Destroy(equippedGun.gameObject);
        }

        // 实例化新枪支
        equippedGun = Instantiate(gunToEquip, weaponHold.position, weaponHold.rotation);
        equippedGun.transform.parent = weaponHold;
    }

    public void EquipGunByName(string gunName)
    {
        // 在列表中查找匹配的枪支
        foreach (GunData gunData in gunList)
        {
            if (gunData.gunName == gunName)
            {
                EquipGun(gunData.gunPrefab);
                return;
            }
        }

        Debug.LogWarning($"Gun with name '{gunName}' not found in the gun list!");
    }

    public void Shoot()
    {
        // 如果当前有装备枪支，触发射击
        if (equippedGun != null)
        {
            equippedGun.Shoot();
        }
    }
}