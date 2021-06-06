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
/*
$vars=Values.ArrayRange(1,2)
$xs=$vars->Position.x
$ys=$vars->Position.y
$zs=$vars->Position.z
ZipWith(sqrt({0}*{0}+{1}*{1}+{2}*{2}),$xs,$ys,$zs).Rename(M).Fold({0}+{1})
*/

// The next 3 are equivalent
/*
$n=StlStructs.members(.size())
StlStructs.Array($n)
*/
/*
function #StlVector()
$n=$this.members(.size())
$this.Array($n)
end
StlStructs.#StlVector()
*/
/*
import(stl)
StlStructs.#StlVector()
*/

// Unusual use of the syntax, but tests different types of arguments
/*
import(stl)
function #FilterVector($vector,$filter)
$vector.#StlVector().FilterString({0},$filter)
end
$varname=StlStructs
#FilterVector($varname,cat)
*/

#include <iostream>
#include <math.h>
#include <stdlib.h>
#include <string>
#include <vector>

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

struct StlStruct
{
	std::string Name;
	Vector Position;
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

	srand(0);
	std::vector<StlStruct> StlStructs;
	for (int i = 0; i < 100; ++i)
	{
		StlStruct NewStruct;
		NewStruct.Name = rand() % 2 ? "cat" : "dog";
		NewStruct.Position.x = rand() / static_cast<float>(RAND_MAX);
		NewStruct.Position.y = rand() / static_cast<float>(RAND_MAX);
		NewStruct.Position.z = rand() / static_cast<float>(RAND_MAX);
		StlStructs.push_back(NewStruct);
	}

	sqrt(2.0f);	// To force the debugger to be able to execute sqrt().
}