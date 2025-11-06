using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
public class NewMonoBehaviourScript : MonoBehaviour
{
    // Global variables
    public GameObject stemPrefab;   //Varsi gameobject jota käytetään L-systeemissä.  
    public GameObject leafPrefab;  //Lehti gameobject jota kätetään L-systeemissä. 
    public Tilemap tilemap; //Tilemap
    public TileBase wallTile; //Seinä
    public TileBase floorTile; //Lattia
    int[,] map; //Kartta on periaatteessa 2D lista. 
    int WIDTH = 80; //Kartan leveys.
    int HEIGHT = 80; //Kartan korkeus.
    System.Random rand = new System.Random(); //Randomn objeckti, jolla saadaan satunnaiset arvot.
    string toChangeByRules = "F"; //Muutettava kirjain, tällä kertaa F. 
    public Vector3 startPos = Vector3.zero; //Aloitus paikka piirtämiselle. 
     public float stepSize = 0.1f; //Koko miten paljo tila liikkuu eteenpäin piirroksessa.
    public float angle = 25f; //Rotaation kulma
     
    Dictionary<char, List<string>> rules = new() //Kielioppi sääntö, miksi kirjain F muutetaan, kun se tavataan l-system metodissa. Variaatiota.
    {
        { 'F', new List<string>
            {
                "F+F--++[-F]F[F]",
                "F[-F]F[+F]F",
                "FF+[+F-F-F]-[-F+F+F]",
                "F[+F]F[-F]F",
                "FF-[-F+F+F]+[+F-F-F]"
            }
        }
    };

    //Structure, jossa säilytetään paikka ja rotaatio infoa DrawLsystem metodille. 
    struct TransformInfo
    {
    public Vector3 position; //Paikka variable, joka säilyttää paikan Vector3 muoodssa, eli Unity maailman koodinaateissa
    public Quaternion rotation; //Rotation variable, joka säilyttää miten paljon rotaatiota on. 

    public TransformInfo(Vector3 pos, Quaternion rot) //Metodi jolla luodaan uusi objeckti, objektin kautta saadaan variable talteen saven jälkeen.
        {
            position = pos;
            rotation = rot;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() //Start funktio, joka käy kun painetaan play Unityn sisällä. 
    {
        GenerateMap(); //Luodaan kartta generatemap metodilla proseduraaalisesti.
        DrawMap(); //Piirretään kartta DrawMap metodilla.
        SpawnPlants(); //Luo kasvit karttaan.
    }

    // Update is called once per frame
    //Metodi tehdä karttoja proceduraalisesti. Algoritmi jota käytettiin: Cellular automata. Enemmän alhaalla.
    void DrawMap()
    {
        tilemap.ClearAllTiles(); //Kaikki tilemapin tilet clearataan
        for (int x = 0; x < WIDTH; x++) //Käydään kartta kartta/lista läpi columni kerrallaan.
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);  //Luodaan uusi Vector3Int, joka on paikka listan koordinaattien mukaan. Eli paikka unity maailmassa.
                if (map[x, y] == 1) //Onko kartan arvo 1, jos on -> seinä, else lattia.
                {
                    tilemap.SetTile(tilePosition, wallTile); 
                }
                else
                {
                    tilemap.SetTile(tilePosition, floorTile);
                }
            }
        }
    }

    // Tarkistaa onko maailman koordinaatti lattia vai ei ja onko listan/kartan sisällä.
    bool IsWorldPosFloor(Vector3 worldPos)
        {
            if (tilemap == null) return false; //Jos ei ole tilemappia, palauttaa false.
            Vector3Int cell = tilemap.WorldToCell(worldPos); //Muuttaa maailman koordinaatit kartan koordinaateiksi.
            if (cell.x < 0 || cell.y < 0 || cell.x >= WIDTH || cell.y >= HEIGHT) return false; //Tarkistaa onko koordinaatit listan sisällä.
            return map[cell.x, cell.y] == 0; //Palauttaa boolen arvon, true tai false, true, jos listan arvo on 0.
        }
    Vector3 GetRandomStartPos() //Saadaan randomn paikka kartassa/listalla, tarkistetaan, että se on lattia. 
    {                           //Jos lattia, otetaan alin lattia kunnes tulee maata/seinä vastaan. 
        int maxTries = 5000; //Maksimi yritykset löytää paikk, ettei jää jumiin.


        for (int tries = 0; tries < maxTries; tries++) 
        {
            int tx = UnityEngine.Random.Range(0, WIDTH); //Satunnainen paikka.
            int ty = UnityEngine.Random.Range(0, HEIGHT);//Satunnainen paikka.
            if (map[tx, ty] == 0) //Tarkistetaan onko lattia.
            {
                for(int y = ty; y >= 0; y--)  //Alaspäin kunnes lattia vastaan tai kunnes loppuu lista. 
                {
                    if (map[tx, y] == 1) //Lattia, mennään, otetaan yks ylempi paikka ja palautetaan se.
                    {
                        ty = y + 1;
                        Vector3Int c = new Vector3Int(tx, ty, 0); //Vector3 koordinaatit tilemapissa, eli unity maailmassa.
                        Vector3 cell2 = tilemap.GetCellCenterWorld(c); //Keskikohta tilemapissa, eli laatan/solun keskikohta.
                        return cell2 - Vector3.up * (tilemap.cellSize.y * 0.5f); //Miinustetaan puolet laatan korkeudesta, jotta kasvi menee pohjalle. 
                    }
                    else if (y == 0) //Jos viimeinen, plantataan sinne.
                    {
                        if (map[tx, y] == 0)
                        {
                            Vector3Int c = new Vector3Int(tx, ty, 0); //Samat mitä äsken.
                            Vector3 cell2 = tilemap.GetCellCenterWorld(c);
                            return cell2 - Vector3.up * (tilemap.cellSize.y * 0.5f);
                        }
                    }
                }
            }
        }

        return Vector3.zero; //Palauttaa ainakin jotain, jos ei löydy paikkaa, palauttaa (0,0,0) koordinaatit
    }

public int plantCount = 20; //Kuinka monta kasvia luodaan per kartta. 

void SpawnPlants()
{
    for (int i = 0; i < plantCount; i++) //Luomis for loop. 
    {
        startPos = GetRandomStartPos(); //Uusi satunnainen paikka kasville, listan sisällä ja lattia.
        string sequence = generateStrings("F", 2); //Kirja kielioppisäännöillä uudeksi sanamuodostelmaksi.
        DrawLsystem(sequence); //Piirretään kasvi l-systeemillä. 
    }
}
    void GenerateMap()
    {
        //Paikalliset muuttujat.
        map = new int[WIDTH, HEIGHT]; //Alkuperäinen kartta, joka näytetään.


        //int cellChanceToLive = rand.Next(); //Alussa, satunnainen mahdollisuus kartan  kohtien (solut) olla, joko kuolleita(0) tai eläviä(1).
        int caIterations = 5;//Satunnainen määrä kokonaisia toistoja, 2-4.rand.Next(2, 5);
        for (int columnIter = 0; columnIter < WIDTH; columnIter++) //Ensimmäinen loop, tässä lisätään satunnaiset arvot (0-1) karttaan (map) Käytään Column kerrallaan läpi. 
        {
            for (int rowIter = 0; rowIter < HEIGHT; rowIter++)
            {
                double randomValue = rand.NextDouble(); //Satunnainen arvo kaikille kartan kohdille. Vaikuttaa siis siihen tuleeko 0 vai 1.
                double cellChanceToLive = 0.45;
                if (randomValue < cellChanceToLive) //Satunnaisuus tarkistus, sen perusteella map(x,y) = 0 tai 1.
                {
                    map[columnIter, rowIter] = 0;
                }
                else
                {
                    map[columnIter, rowIter] = 1;
                }
            }
        }
        for (int wholeIter = 0; wholeIter <= caIterations; wholeIter++)
        { //Loop jolla itse vaihdot soluautomaatiolla tehdään. 
            int[,] tempMap = new int[WIDTH, HEIGHT]; //Väliaikainen kartta, johon tehdään loopeissa muutoksia Cellular automatan avulla.
            for (int columnIter = 0; columnIter < WIDTH; columnIter++)        //2-4 kokonaista toistoa ja samalla tavalla Columni kerrallaan.
            {
                for (int rowIter = 0; rowIter < HEIGHT; rowIter++)
                {
                    //Paikalliset muuttujat
                    int aliveNeighbours = 0;  //Miten monta alive (1 arvoa) naapuri kartan kohdilla (soluilla).
                    //Tarkistetaan meneekö out of bounds, vai onko toimiva naapuri. 
                    //Jos naapuri ei ole out of bounds tarkistetaan onko elossa. 
                    //Loopataan kahdessa for loopissa -1 ja 1 välillä, tällä tavalla saadaan tarvittavat naapurien arvot.
                    //Ensimmäinen loop käy vasemmat naapurit, ylhäältä alas, toinen keski naapurit ja sitten lopuksi oikeat naapurit. 
                    for (int columnLoopArvo = -1; columnLoopArvo <= 1; columnLoopArvo++)
                    {
                        for (int rowLoopArvo = -1; rowLoopArvo <= 1; rowLoopArvo++)
                        {
                            if (columnLoopArvo == 0 && rowLoopArvo == 0) //Jos keskipiste, ignore koska se ei ole naapuri.
                            {
                                continue;
                            }
                            int ix = columnIter + columnLoopArvo;
                            int jx = rowIter + rowLoopArvo;

                            if (ix >= 0 && jx >= 00 && ix < WIDTH && jx < HEIGHT) //Tarkistetaan bounds ja sitten onko arvo 1, jos on kasvatetaan aliveNeighbours.
                            {
                                if (map[ix, jx] == 1)
                                {
                                    aliveNeighbours++;
                                }
                            }
                        }
                    }
                    //Ja täällä muutetaan itse solun arvoa riippuen itse solun omasta arvosta, sekä siitä,
                    //miten monta elossa olevaa naapuri solua solulla on. 
                    /* if (map[columnIter, rowIter] == 1)
                     {
                         if (aliveNeighbours < 2)
                         {
                             tempMap[columnIter, rowIter] = 0;
                         }
                         else if (aliveNeighbours == 2 || aliveNeighbours == 3)
                         {
                             tempMap[columnIter, rowIter] = 1;
                         }
                         else if (aliveNeighbours > 3)
                         {
                             tempMap[columnIter, rowIter] = 0;
                         }
                     }
                     else if ((map[columnIter, rowIter] == 0) && (aliveNeighbours == 3))
                     {
                         tempMap[columnIter, rowIter] = 1;
                     }*/
                    /*Console.WriteLine(aliveNeighbours);*/
                    //Ensimmäinen algoritmi
                    /*if (map[columnIter, rowIter] == 1) 
                    {
                        if (aliveNeighbours < 2)
                        {
                            tempMap[columnIter, rowIter] = 0;
                        }
                        if(aliveNeighbours == 2 || aliveNeighbours == 3)
                        {
                            tempMap[columnIter, rowIter] = 1;
                        }
                        if(aliveNeighbours > 3)
                        {
                            tempMap[columnIter, rowIter] = 0;
                        }
                    }
                    else if ((map[columnIter, rowIter] == 0) && (aliveNeighbours == 3))
                    {
                        tempMap[columnIter, rowIter] = 1;
                    } */
                    /*
                    //Toinen Algoritmi
                    if (map[columnIter, rowIter] == 0)
                    {
                        if (aliveNeighbours > 4)
                        {
                            tempMap[columnIter, rowIter] = 1;
                        }
                    }                    
                    else
                    {
                        if (aliveNeighbours < 4)
                        {
                            tempMap[columnIter, rowIter] = 0;
                        }
                    }*/
                    if (map[columnIter, rowIter] == 1)  //Asuttamis säännöt 
                    {
                        if (aliveNeighbours >= 4)
                        {
                            tempMap[columnIter, rowIter] = 1;
                        }
                        else
                        {
                            tempMap[columnIter, rowIter] = 0;
                        }
                    }
                    else
                    {
                        if (aliveNeighbours >= 5)
                        {
                            tempMap[columnIter, rowIter] = 1;
                        }
                        else
                        {
                            tempMap[columnIter, rowIter] = 0;
                        }
                    }
                }
            }
            for (int x = 0; x < WIDTH; x++) //Kopikoidaan temp kartta oikeaan karttaan.
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    map[x, y] = tempMap[x, y];
                }
            }
        }
    }
    //Metodi, jolla muodostetaan L-system kielioppi sana, jonka avulla voidaan muodostaa kasveja.
    string generateStrings(string input, int interations)
    {
        string output = input; //Otetaan sana talteen, joka muutetaan kielioppi säännöillä uudeksi sanaksi.
        for (int i = 0; i < interations; i++) //Määrättyjen iteraatioiden verran, sanaa muutetaan kielioppi sääntöjen mukaan. Kirjain muuttuu uudeksi kirjain rypäleeksi.
        {
            string nextFullString = "";  //Sana muutetaan kielioppisääntöjen mukaan nextFullString variableen aina jokaisella iteraatiolla.
            foreach (char c in output)  //Käydään läpi jokainen merkki sanassa output.
            {
                nextFullString += rules.ContainsKey(c)
                    ? rules[c][UnityEngine.Random.Range(0, rules[c].Count)]  // pick random replacement
                    : c.ToString(); //Jokainen output merkki muutetaan kielioppisääntöjen mukaan ja 
            }                                                                  //jokainen uusi merkki rypäs liitetään nextFullString variableen.
            output = nextFullString;  //Jokaisen iteraation jälkeen kootaan uusi sana outputtiin, ja sitten koko iteraatio alusta
        }                             //niin monesti kuin iteraatioita on tehtävä.
        return output;                //Palautetaan kielioppien kautta tehty uusi sana.
    }

    void DrawLsystem(string sequence)
    {
        Stack<TransformInfo> stack = new(); //Luodaan stack jossa säilytetään muutos ohjeet.
        Vector3 position1 = startPos; //Nykyinen piirros paikka unity maailmassa Vector3 koordinaateissa. 
        Quaternion rotation1 = Quaternion.identity; //Nykyinen käännös.

        foreach (char c in sequence) //Käydään jokainen merkki sequence sanassa läpi.
        {
            switch (c)
            {
                case 'F': //Liikkuu eteenpäin stepSize määrän ja luo osan "kasvia".  
                    Vector3 newPosition = position1 + rotation1 * Vector3.up * stepSize; //Uusi paikka, lasketaan nykyisen Vector3 paikasta, 
                                                                                          //rotaatiosta ja stepSize muuttujasta. Vector3.up on vain koordinaatti
                                                                                          //ylös suuntaan (0,1,0), joten siitä kerrotaan stepsize, saadaan pieni
                                                                                          //muutos ylös päin ja rotaatiolla käännetään jonnekki suuntaan.
                    Vector3 middlePoint = (position1 + newPosition) / 2; //Middle point tarkistus, jotta varmistetaan että keskikohta on myös lattia.
                    if (!IsWorldPosFloor(position1) || !IsWorldPosFloor(middlePoint) || !IsWorldPosFloor(newPosition))
                        {
                            //position1 = newPosition;
                            break; //Jos lattia, ei tehdä mitään.
                        }//Uusi paikka.
                    SpawnJuuri(position1, newPosition); //Piirtäminen/"luominen"
                    position1 = newPosition; //Alkuperäinen paikka muuttuu uudeksi paikaksi.
                    break;
                case '+': //Pyöritetään, rotate nykyistä piirros kulmaa.
                    rotation1 *= Quaternion.Euler(0, 0, angle);
                    break;
                case '-':
                    rotation1 *= Quaternion.Euler(0, 0, -angle);
                    break;
                case '[': //Tallennetaan tila/paikka ja rotaatio.
                    stack.Push(new TransformInfo(position1, rotation1));
                    break;
                case ']': //Otetaan tallennus ulos stackistä. Saadaan tilat ylös, rotaatio ja paikka.
                    var infoOnSaveState = stack.Pop();
                    position1 = infoOnSaveState.position;
                    rotation1 = infoOnSaveState.rotation;
                    if (!IsWorldPosFloor(position1)) //Jos ei ole lattia, eli jos statement ei ole true, break ja ei tehdä mitään.
                    {
                        break;
                    }
                    else
                        {
                            if (leafPrefab != null)
                            {
                                Instantiate(leafPrefab, position1, rotation1); //Luodaan lehti tähän paikkaan.
                            }
                        } 
                    break;
            }
        }

    }
    void SpawnJuuri(Vector3 start, Vector3 end)
    {
        Vector3 middlePoint = (start + end) / 2; //Keskikohta.
        Vector3 direction = end - start; //Suunta vektori, eli startista endiin.

        GameObject stem = Instantiate(stemPrefab, middlePoint, Quaternion.identity); //Luodaan varsi gameobject keskikohtaan.
        stem.transform.up = direction.normalized; //Asetetaan vectori suunta ylöspäin eli y suuntaan.
        stem.transform.localScale = new Vector3(stem.transform.localScale.x, direction.magnitude, stem.transform.localScale.z); //Skalataan varsi pituuden mukaan.

    }

}

