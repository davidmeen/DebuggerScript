// Example scripts:
//Values.Array(10)->Name.Rename(PlayerName).ArrayRange(1,3)
//Values.Array(10)->Name.Rename(PlayerName)[1]
//Values.ArrayRange(1,2)->Position.x.Reference()
//Values.ArrayRange(1,2)->Position.Members(.x,.y,.z).ReinterpretCast(int)
//Values.Memory(-10,20)
//Values.Array(10).FilterString({0}->Name,You)->Position
//Values.Array(10).FilterNotString({0}->Name,You)->Position
/*
$filtered=Values.Array(10).Filter({0}!=0)
$xs=$filtered->Position.x
$ys=$filtered->Position.y
$zs=$filtered->Position.z
Zip($xs,$ys,$zs)
*/
/*
$filtered=Values.Array(10).Filter({0}!=0)
$xs=$filtered->Position.x
$ys=$filtered->Position.y
$zs=$filtered->Position.z
Concat($xs,$ys,$zs)
*/
/*
$filtered=Values.Array(10).Filter({0}!=0)
$xs=$filtered->Position.x
$ys=$filtered->Position.y
$zs=$filtered->Position.z
ZipWith(sqrt({0}*{0}+{1}*{1}+{2}*{2}),$xs,$ys,$zs).Rename(Magnitude)
*/

#include <iostream>
#include <math.h>

struct Vector
{
	float x, y, z;
};

struct Player
{
	char Name[8];
	Vector Position;
	Vector Facing;
};

Player Player1;
Player Player2;
Player* Values[10];

int main()
{
	strcpy_s(Player1.Name, "Me");
	Player1.Position.x = 1.0f;
	Player1.Position.y = 2.0f;
	Player1.Position.z = 3.0f;

	strcpy_s(Player2.Name, "You");
	Player2.Position.x = 4.5f;
	Player2.Position.y = 6.7f;
	Player2.Position.z = 8.9f;

	Values[1] = &Player1;
	Values[2] = &Player2;

	sqrt(2.0f);	// To force the debugger to be able to execute sqrt().
}