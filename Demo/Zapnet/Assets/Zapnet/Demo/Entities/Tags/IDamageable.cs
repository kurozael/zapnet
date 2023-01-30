public interface IDamageable
{
	void TakeDamage(int damage);
	void Kill();
    int GetMaxHealth();
    int GetHealth();
}
