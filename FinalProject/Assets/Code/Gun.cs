using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{

    //getting the position of the bullet
    public Transform muzzle;
    public Bullet bullet;
    public float msBetweenShots = 100;
    public float muzzleVelocity = 35;
     float nextShotTime;

    public void Shoot()
    {
        if (Time.time > nextShotTime)
        {
            nextShotTime = Time.time + msBetweenShots / 1000;
            Bullet newBullet = Instantiate(bullet, muzzle.position, muzzle.rotation) as Bullet;
            newBullet.SetSpeed(muzzleVelocity);
        }
    }
       
   

}
