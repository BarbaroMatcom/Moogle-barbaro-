using System.Text.RegularExpressions;
using System.IO;
namespace MoogleEngine;


public static class Moogle
{
    static Dictionary<string, Dictionary<string, float>> documentos_procesados = new Dictionary<string, Dictionary<string, float>>();//
                                                                                                                                     //diccionario para almacenar los documentos con los 
    static Dictionary<string, int> CantidadDocPalabras = new Dictionary<string, int>();//diccionario que guarda por cada palabra la cantidad de documentos en que aparece
    static float[,] voc_matrix;//matriz del sistema donde se almacena el tf-idf
    static List<string> vocabulario;//vocabulario donde se guardan todas las palabras del corpus

    public static void Load() // metodo para cargar los documentos 
    {


        string[] rutasDocumentos = Directory.GetFiles(System.IO.Path.GetFullPath("../Content")); //guardo los nombres de todos los archivos



        vocabulario = new List<string>();//inicializo el vocabulario
        foreach (string rutaDocumento in rutasDocumentos) //por cada documento
        {
            string contenido = File.ReadAllText(rutaDocumento);//leo todo el texto

            string[] palabrasDocumento = procesar_palabras(contenido); //proceso las palabras y las separo en un array
            Dictionary<string, float> currentDocument = sacar_diccionario(palabrasDocumento);//creo el diccionario con el tf 
            documentos_procesados.Add(rutaDocumento, currentDocument);//guardo el documento con todos sus datos (palabras , tf)
        }

        foreach (string doc in documentos_procesados.Keys)//por cada documento
        {
            bool aparecio = false; //bandera para saber si aparecio 
            Console.WriteLine("viendo el documento {0} ", doc);
            foreach (string wrd in documentos_procesados[doc].Keys)//por cada palabra en el documento
            {
                if (!CantidadDocPalabras.ContainsKey(wrd))//si no se ha registrado la palabra la guardo con un uno
                {
                    CantidadDocPalabras.Add(wrd, 1);
                    aparecio = true;//marco como registrada para no contar 2 veces
                }
                else if (!aparecio) // si no ha aparecido pero esta guardada le sumo 1
                {
                    CantidadDocPalabras[wrd]++;
                    aparecio = true;
                }

                if (!vocabulario.Contains(wrd))//si no esta en el vocabulario la agrego
                {
                    vocabulario.Add(wrd);
                }
            }
        }

        voc_matrix = new float[documentos_procesados.Count(), vocabulario.Count];//inicializando la matriz del sistema

        int cont = 0;
        foreach (Dictionary<string, float> doc in documentos_procesados.Values)//por cada documento
        {
            float[] tmp = Vectorizar(doc, vocabulario.ToArray());//lo vectorizo
            for (int i = 0; i < tmp.Length; i++)
            {
                voc_matrix[cont, i] = tmp[i];//guardo en la matriz los datos
            }
            cont++;
        }


    }
    public static SearchResult Query(string query)
    {
        // Modifique este método para responder a la búsqueda
        string[] palabras_query = procesar_palabras(query); //proceso las palabras del query
        Dictionary<string, float> aux = sacar_diccionario(palabras_query); // creo el diccionario del tf
        float[] vectorquery = Vectorizar(aux, vocabulario.ToArray());//vectorizo como si fuera un documento
        int doc_numb = 0;

        string[] result_docs = new string[documentos_procesados.Keys.Count]; //inicializando el vector de documentos
        float[] scores = new float[result_docs.Length]; // el vector de los scores

        foreach (string doc_name in documentos_procesados.Keys)//por cada documento
        {
            result_docs[doc_numb] = doc_name;//guardo el nombre del documento
            scores[doc_numb] = Calcular_score(voc_matrix, vectorquery, doc_numb); //guardo su score
            doc_numb++;//iterador para saber la posicion en el array
        }

        Sort(result_docs, scores);//ordeno de mayor a menor 
        SearchItem[] items = new SearchItem[result_docs.Length];//array de SearchItem

        for (int i = 0; i < result_docs.Length; i++)
        {
            items[i] = new SearchItem(GetDocName(result_docs[i]), read_snippet(result_docs[i]), scores[i]);//por cada uno saco el nombre score y snippet
        }

        return new SearchResult(items, query);
    }

    static string GetDocName(string doc)//metodo para sacar el nombre del documento
    {
        return doc.Split("/").Last().Split(".")[0];//originalmente son ../Content/Nombre.txt separas por / , te quedas con el ultimo 
    }                                       //que seria Nombre.txt , separas por . y t quedas con el 1ro que seria Nombre


    static string read_snippet(string docname)//metodo para sacar el snippet
    {
        string primeras_1000_lineas = "";
        StreamReader sr = new StreamReader(docname);
        string lect = sr.ReadToEnd();//leo el archivo

        if (lect.Length < 1000) // si tiene menos de 1000 caracteres
        {
            return lect;//devuelvo todo el documentos
        }

        for (int i = 0; i < 1000; i++) //leo 1000caracteres
        {
            primeras_1000_lineas += lect[i];
        }
        return primeras_1000_lineas;
    }
    public static void Sort(string[] docs, float[] scores)//metodo burbuja para ordenar los documentos
    {
        for (int i = 0; i < scores.Length; i++)// garantiza que ordene
        {
            for (int j = 0; j < scores.Length - 1; j++)//recorre el array
            {
                if (scores[j] < scores[j + 1])//si el de atras es menor
                {
                    string aux = docs[j];
                    float tmp = scores[j];

                    scores[j] = scores[j + 1]; // se cambian de lugar
                    docs[j] = docs[j + 1];

                    scores[j + 1] = tmp;
                    docs[j + 1] = aux;
                }
            }
        }
    }

    public static float Calcular_score(float[,] tf_matrix, float[] vect_query, int docnumber)//calcular el score de un documento respecto al query
    {
        float aux = 0;
        float dim1 = 0;
        float dim2 = 0;
        for (int i = 0; i < vect_query.Length; i++)//por cada elemento del query
        {
            aux += tf_matrix[docnumber, i] * vect_query[i]; //producto punto entre documento y query
            dim1 += (float)Math.Pow(tf_matrix[docnumber, i], 2); //para calcular la norma del vector 1
            dim2 += (float)Math.Pow(vect_query[i], 2);//para calcular la norma del vector 2
        }
        return aux / ((dim1 == 0 || dim2 == 0) ? 1 : (float)(Math.Sqrt(dim1) * Math.Sqrt(dim2)));//distancia coseno 
    }

    public static string[] procesar_palabras(string contenido)//metodo para separar palabras
    {
        contenido = contenido.ToLower();//llevo a minusculas
        Regex reg = new Regex("[^a-zA-Z0-9]");//elimino caracteres innecesarios
        contenido = reg.Replace(contenido, " ");//separo en un array de palabras

        string[] palabrasDocumento = contenido.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return palabrasDocumento;
    }

    public static float[] Vectorizar(Dictionary<string, float> doc_info, string[] vocabulario)//metodo para llevar
    {
        float[] vector = new float[vocabulario.Length];//crea el vector del tamanno del vocabulario
        for (int i = 0; i < vocabulario.Length; i++)
        {
            if (doc_info.ContainsKey(vocabulario[i]))
            {
                //rellena el vector con el tf-idf de cada palabra y lo almacena en el vector
                vector[i] = doc_info[vocabulario[i]] * Calcular_IDF(documentos_procesados.Count, CantidadDocPalabras[vocabulario[i]]);
            }
        }
        return vector;
    }

    public static Dictionary<string, float> sacar_diccionario(string[] palabrasDocumento)//metodo para crear el diccionario conel tf de cada palabra por documento
    {

        Dictionary<string, float> currentDocument = new Dictionary<string, float>();// inicializa el documento
        foreach (var palabra in palabrasDocumento) //por cada palabra en el documento
        {
            if (currentDocument.ContainsKey(palabra))//si tiene la palabra
            {
                currentDocument[palabra]++; //aumenta su tf
            }
            else // si no estaba la agrego con tf 1
            {
                currentDocument.Add(palabra, 1);
            }
        }
        return currentDocument;
    }
    public static float Calcular_IDF(int N, int n)//metodo para calcular el idf
    {
        float idf = (float)Math.Log10(N / n);//formula N es la cantidad de documentos y n la cantidad donde aparece la palabra
        return idf;
    }

}
