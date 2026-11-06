using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public static class SDKAssetHandler
{
    public static async UniTask OpenCommonConfirmTipsPanel(CommonConfirmTipsPanel.Args args)
    {
        const string key = "SDKPanel/CommonConfirmTipsPanel";

        GameObject go = await Addressables.InstantiateAsync(key).ToUniTask();

        var panel = go.GetComponent<CommonConfirmTipsPanel>();

        if (panel == null)
        {
            Debug.LogError($"Prefab上没有 CommonConfirmTipsPanel 组件：{key}");
            Addressables.ReleaseInstance(go);
            return;
        }

        panel.OnOpen(args);
    }
}

public class CommonConfirmTipsPanel : MonoBehaviour
{
    private const string SelectedLanguageKey = "SelectedLanguage";
    private const string DefaultLanguage = "en-US";

    private static readonly Dictionary<string, Dictionary<string, string>> LanguageMap =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        {
            {
                "HTTPNetworkProblem",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "网络错误，请稍后再试" },
                    { "en-US", "Network error, please try again later." },
                    { "pt-BR", "Erro de rede, tente novamente mais tarde." },
                    { "id-ID", "Kesalahan jaringan, silakan coba lagi nanti." },
                    { "ru-RU", "Ошибка сети, попробуйте позже." },
                    { "ja-JP", "ネットワークエラーが発生しました。しばらくしてからもう一度お試しください。" },
                    { "ko-KR", "네트워크 오류입니다. 잠시 후 다시 시도해 주세요." },
                    { "es-ES", "Error de red, inténtalo de nuevo más tarde." },
                    { "es-MX", "Error de red, inténtalo más tarde." },
                    { "tr-TR", "Ağ hatası, lütfen daha sonra tekrar deneyin." },
                    { "en-PH", "Network error, please try again later." },
                    { "vi-VN", "Lỗi mạng, vui lòng thử lại sau." },
                }
            },
            {
                "Tips_NetworkError",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "网络连接异常，请检查网络" },
                    { "en-US", "Network connection error, please check your network" },
                    { "pt-BR", "Erro de conexão de rede, verifique sua rede" },
                    { "id-ID", "Koneksi jaringan bermasalah, silakan periksa jaringan" },
                    { "ja-JP", "ネットワーク接続エラーです。ネットワーク接続を確認してください。" },
                    { "ko-KR", "네트워크 연결 오류입니다. 네트워크 상태를 확인해 주세요." },
                    { "ru-RU", "Ошибка сетевого подключения. Проверьте подключение к сети." },
                }
            },
            {
                "TokenClient_VPN",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "请关闭VPN或代理后重试。" },
                    { "en-US", "Please disable your VPN or proxy and try again." },
                    { "pt-BR", "Desative a VPN ou o proxy e tente novamente." },
                    { "id-ID", "Harap nonaktifkan VPN atau proxy, lalu coba lagi." },
                    { "ja-JP", "VPNまたはプロキシを無効にしてから、もう一度お試しください。" },
                    { "ko-KR", "VPN 또는 프록시를 비활성화한 후 다시 시도해 주세요." },
                    { "es-MX", "Desactiva la VPN o el proxy e inténtalo de nuevo." },
                    { "ar-SA", "يرجى تعطيل شبكة VPN أو الخادم الوكيل ثم المحاولة مرة أخرى." },
                    { "de-DE", "Bitte deaktivieren Sie Ihr VPN oder Ihren Proxy und versuchen Sie es erneut." },
                    { "ru-RU", "Пожалуйста, отключите VPN или прокси-сервер и повторите попытку." },
                }
            },
            {
                "TokenClient_REGION",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "所在地区暂不支持该服务。" },
                    { "en-US", "This service is currently unavailable in your region." },
                    { "pt-BR", "Este serviço não está disponível na sua região no momento." },
                    { "id-ID", "Layanan ini saat ini tidak tersedia di wilayah Anda." },
                    { "ja-JP", "現在、お住まいの地域ではこのサービスを利用できません。" },
                    { "ko-KR", "현재 해당 지역에서는 이 서비스를 이용할 수 없습니다." },
                    { "es-MX", "Actualmente, este servicio no está disponible en tu región." },
                    { "ar-SA", "هذه الخدمة غير متاحة حاليًا في منطقتك." },
                    { "de-DE", "Dieser Dienst ist derzeit in Ihrer Region nicht verfügbar." },
                    { "ru-RU", "В настоящее время этот сервис недоступен в вашем регионе." },
                }
            },
            {
                "TokenClient_APP_INTEGRITY",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "当前客户端环境异常。" },
                    { "en-US", "The current client environment is abnormal." },
                    { "pt-BR", "O ambiente atual do aplicativo está anormal." },
                    { "id-ID", "Lingkungan aplikasi saat ini terdeteksi tidak normal." },
                    { "ja-JP", "現在のクライアント環境に異常が検出されました。" },
                    { "ko-KR", "현재 클라이언트 환경에 이상이 감지되었습니다." },
                    { "es-MX", "Se detectó una anomalía en el entorno actual del cliente." },
                    { "ar-SA", "تم اكتشاف خلل في بيئة العميل الحالية." },
                    { "de-DE", "In der aktuellen Client-Umgebung wurde eine Anomalie erkannt." },
                    { "ru-RU", "Текущая среда клиента работает некорректно." },
                }
            },
            {
                "TokenClient_TOKEN_INVALID",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "稍后重试。" },
                    { "en-US", "Please try again later." },
                    { "pt-BR", "Tente novamente mais tarde." },
                    { "id-ID", "Silakan coba lagi nanti." },
                    { "ja-JP", "しばらくしてからもう一度お試しください。" },
                    { "ko-KR", "잠시 후 다시 시도해 주세요." },
                    { "es-MX", "Inténtalo de nuevo más tarde." },
                    { "ar-SA", "يرجى المحاولة مرة أخرى لاحقًا." },
                    { "de-DE", "Bitte versuchen Sie es später erneut." },
                    { "ru-RU", "Пожалуйста, повторите попытку позже." },
                }
            },
            {
                "TokenClient_Default",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "zh-CN", "稍后重试。" },
                    { "en-US", "Please try again later." },
                    { "pt-BR", "Tente novamente mais tarde." },
                    { "id-ID", "Silakan coba lagi nanti." },
                    { "ja-JP", "しばらくしてからもう一度お試しください。" },
                    { "ko-KR", "잠시 후 다시 시도해 주세요." },
                    { "es-MX", "Inténtalo de nuevo más tarde." },
                    { "ar-SA", "يرجى المحاولة مرة أخرى لاحقًا." },
                    { "de-DE", "Bitte versuchen Sie es später erneut." },
                    { "ru-RU", "Пожалуйста, повторите попытку позже." },
                }
            },
        };

    private sealed class LanguagePack
    {
        public LanguagePack(
            string httpNetworkProblem,
            string networkError,
            string vpn,
            string region,
            string appIntegrity,
            string tryAgain,
            string confirm)
        {
            HttpNetworkProblem = httpNetworkProblem;
            NetworkError = networkError;
            Vpn = vpn;
            Region = region;
            AppIntegrity = appIntegrity;
            TryAgain = tryAgain;
            Confirm = confirm;
        }

        public string HttpNetworkProblem { get; }
        public string NetworkError { get; }
        public string Vpn { get; }
        public string Region { get; }
        public string AppIntegrity { get; }
        public string TryAgain { get; }
        public string Confirm { get; }

        public string GetText(string key)
        {
            switch (key)
            {
                case "HTTPNetworkProblem": return HttpNetworkProblem;
                case "Tips_NetworkError": return NetworkError;
                case "TokenClient_VPN": return Vpn;
                case "TokenClient_REGION": return Region;
                case "TokenClient_APP_INTEGRITY": return AppIntegrity;
                case "TokenClient_TOKEN_INVALID":
                case "TokenClient_Default": return TryAgain;
                default: return null;
            }
        }
    }

    // 原弹窗未覆盖的本地地区语言。键使用 BCP-47 的语言前缀，多个国家可共用一套文案。
    private static readonly Dictionary<string, LanguagePack> AdditionalLanguagePacks =
        new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase)
        {
            { "sq", new LanguagePack("Gabim rrjeti, ju lutemi provoni përsëri më vonë.", "Gabim në lidhjen e rrjetit, kontrolloni rrjetin tuaj.", "Çaktivizoni VPN-në ose proxy-n dhe provoni përsëri.", "Ky shërbim aktualisht nuk është i disponueshëm në rajonin tuaj.", "Mjedisi aktual i klientit është jonormal.", "Ju lutemi provoni përsëri më vonë.", "Konfirmo") },
            { "hy", new LanguagePack("Ցանցային սխալ է, կրկին փորձեք ավելի ուշ։", "Ցանցային կապի սխալ է, ստուգեք ձեր ցանցը։", "Անջատեք VPN-ը կամ պրոքսին և կրկին փորձեք։", "Այս ծառայությունն այժմ հասանելի չէ ձեր տարածաշրջանում։", "Հաճախորդի ընթացիկ միջավայրում խնդիր է հայտնաբերվել։", "Կրկին փորձեք ավելի ուշ։", "Հաստատել") },
            { "az", new LanguagePack("Şəbəkə xətası, sonra yenidən cəhd edin.", "Şəbəkə bağlantısı xətası, şəbəkənizi yoxlayın.", "VPN və ya proksini söndürüb yenidən cəhd edin.", "Bu xidmət hazırda regionunuzda əlçatan deyil.", "Cari müştəri mühitində problem aşkarlandı.", "Sonra yenidən cəhd edin.", "Təsdiq et") },
            { "bs", new LanguagePack("Mrežna greška, pokušajte ponovo kasnije.", "Greška mrežne veze, provjerite mrežu.", "Isključite VPN ili proxy i pokušajte ponovo.", "Ova usluga trenutno nije dostupna u vašoj regiji.", "Otkriven je problem u trenutnom okruženju aplikacije.", "Pokušajte ponovo kasnije.", "Potvrdi") },
            { "bn", new LanguagePack("নেটওয়ার্ক ত্রুটি, পরে আবার চেষ্টা করুন।", "নেটওয়ার্ক সংযোগে ত্রুটি, আপনার নেটওয়ার্ক পরীক্ষা করুন।", "VPN বা প্রক্সি বন্ধ করে আবার চেষ্টা করুন।", "এই পরিষেবাটি বর্তমানে আপনার অঞ্চলে উপলভ্য নয়।", "বর্তমান অ্যাপ পরিবেশে সমস্যা শনাক্ত হয়েছে।", "পরে আবার চেষ্টা করুন।", "নিশ্চিত করুন") },
            { "nl", new LanguagePack("Netwerkfout, probeer het later opnieuw.", "Netwerkverbindingsfout, controleer uw netwerk.", "Schakel uw VPN of proxy uit en probeer het opnieuw.", "Deze service is momenteel niet beschikbaar in uw regio.", "Er is een probleem gedetecteerd in de huidige appomgeving.", "Probeer het later opnieuw.", "Bevestigen") },
            { "fr", new LanguagePack("Erreur réseau, veuillez réessayer plus tard.", "Erreur de connexion réseau, vérifiez votre réseau.", "Désactivez votre VPN ou proxy, puis réessayez.", "Ce service n’est actuellement pas disponible dans votre région.", "Une anomalie a été détectée dans l’environnement actuel de l’application.", "Veuillez réessayer plus tard.", "Confirmer") },
            { "bg", new LanguagePack("Мрежова грешка. Опитайте отново по-късно.", "Грешка в мрежовата връзка. Проверете мрежата си.", "Изключете VPN или прокси сървъра и опитайте отново.", "Тази услуга в момента не е достъпна във вашия регион.", "Открит е проблем в текущата среда на приложението.", "Опитайте отново по-късно.", "Потвърди") },
            { "rn", new LanguagePack("Habaye ikosa ry’umuyoboro, gerageza hanyuma.", "Ihuza ry’umuyoboro rifise ikibazo, suzuma umuyoboro wawe.", "Zimya VPN canke proxy hanyuma wongere ugerageze.", "Iyi serivisi ntiboneka ubu mu karere kawe.", "Habonetse ikibazo mu buryo porogarama ikoreramwo.", "Gerageza hanyuma.", "Emeza") },
            { "ms", new LanguagePack("Ralat rangkaian, sila cuba lagi kemudian.", "Ralat sambungan rangkaian, sila semak rangkaian anda.", "Matikan VPN atau proksi anda dan cuba lagi.", "Perkhidmatan ini tidak tersedia di rantau anda buat masa ini.", "Masalah dikesan dalam persekitaran aplikasi semasa.", "Sila cuba lagi kemudian.", "Sahkan") },
            { "el", new LanguagePack("Σφάλμα δικτύου, δοκιμάστε ξανά αργότερα.", "Σφάλμα σύνδεσης δικτύου, ελέγξτε το δίκτυό σας.", "Απενεργοποιήστε το VPN ή τον διακομιστή μεσολάβησης και δοκιμάστε ξανά.", "Αυτή η υπηρεσία δεν είναι διαθέσιμη στην περιοχή σας αυτήν τη στιγμή.", "Εντοπίστηκε πρόβλημα στο τρέχον περιβάλλον της εφαρμογής.", "Δοκιμάστε ξανά αργότερα.", "Επιβεβαίωση") },
            { "cs", new LanguagePack("Chyba sítě, zkuste to znovu později.", "Chyba síťového připojení, zkontrolujte svou síť.", "Vypněte VPN nebo proxy a zkuste to znovu.", "Tato služba není ve vašem regionu momentálně dostupná.", "V aktuálním prostředí aplikace byl zjištěn problém.", "Zkuste to znovu později.", "Potvrdit") },
            { "da", new LanguagePack("Netværksfejl, prøv igen senere.", "Fejl i netværksforbindelsen, kontrollér dit netværk.", "Deaktivér din VPN eller proxy, og prøv igen.", "Denne tjeneste er ikke tilgængelig i dit område i øjeblikket.", "Der blev registreret et problem i det aktuelle appmiljø.", "Prøv igen senere.", "Bekræft") },
            { "et", new LanguagePack("Võrgutõrge, proovige hiljem uuesti.", "Võrguühenduse tõrge, kontrollige oma võrku.", "Lülitage VPN või puhverserver välja ja proovige uuesti.", "See teenus pole praegu teie piirkonnas saadaval.", "Praeguses rakenduse keskkonnas tuvastati probleem.", "Proovige hiljem uuesti.", "Kinnita") },
            { "am", new LanguagePack("የአውታረ መረብ ስህተት፣ ቆይተው እንደገና ይሞክሩ።", "የአውታረ መረብ ግንኙነት ስህተት፣ አውታረ መረብዎን ያረጋግጡ።", "VPN ወይም ፕሮክሲን ያጥፉና እንደገና ይሞክሩ።", "ይህ አገልግሎት በአካባቢዎ አሁን አይገኝም።", "በአሁኑ የመተግበሪያ አካባቢ ችግር ተገኝቷል።", "ቆይተው እንደገና ይሞክሩ።", "አረጋግጥ") },
            { "fi", new LanguagePack("Verkkovirhe, yritä myöhemmin uudelleen.", "Verkkoyhteysvirhe, tarkista verkkoyhteytesi.", "Poista VPN tai välityspalvelin käytöstä ja yritä uudelleen.", "Tämä palvelu ei ole tällä hetkellä saatavilla alueellasi.", "Nykyisessä sovellusympäristössä havaittiin ongelma.", "Yritä myöhemmin uudelleen.", "Vahvista") },
            { "ka", new LanguagePack("ქსელის შეცდომა. გთხოვთ, მოგვიანებით სცადოთ.", "ქსელთან კავშირის შეცდომა. შეამოწმეთ ქსელი.", "გამორთეთ VPN ან პროქსი და ხელახლა სცადეთ.", "ეს სერვისი ამჟამად თქვენს რეგიონში მიუწვდომელია.", "აპის მიმდინარე გარემოში პრობლემა გამოვლინდა.", "გთხოვთ, მოგვიანებით სცადოთ.", "დადასტურება") },
            { "hr", new LanguagePack("Mrežna pogreška, pokušajte ponovno kasnije.", "Pogreška mrežne veze, provjerite mrežu.", "Isključite VPN ili proxy i pokušajte ponovno.", "Ova usluga trenutačno nije dostupna u vašoj regiji.", "Otkriven je problem u trenutačnom okruženju aplikacije.", "Pokušajte ponovno kasnije.", "Potvrdi") },
            { "ht", new LanguagePack("Erè rezo, tanpri eseye ankò pita.", "Erè koneksyon rezo, tanpri tcheke rezo ou.", "Dezaktive VPN oswa proxy a epi eseye ankò.", "Sèvis sa a pa disponib nan rejyon ou kounye a.", "Yo detekte yon pwoblèm nan anviwònman aplikasyon an.", "Tanpri eseye ankò pita.", "Konfime") },
            { "hu", new LanguagePack("Hálózati hiba, próbálja újra később.", "Hálózati kapcsolati hiba, ellenőrizze a hálózatot.", "Kapcsolja ki a VPN-t vagy a proxyt, majd próbálja újra.", "Ez a szolgáltatás jelenleg nem érhető el az Ön régiójában.", "Problémát észleltünk az alkalmazás jelenlegi környezetében.", "Próbálja újra később.", "Megerősítés") },
            { "he", new LanguagePack("שגיאת רשת, נסו שוב מאוחר יותר.", "שגיאת חיבור לרשת, בדקו את הרשת שלכם.", "השביתו את ה-VPN או את שרת ה-Proxy ונסו שוב.", "שירות זה אינו זמין כרגע באזור שלכם.", "זוהתה בעיה בסביבת האפליקציה הנוכחית.", "נסו שוב מאוחר יותר.", "אישור") },
            { "hi", new LanguagePack("नेटवर्क त्रुटि, कृपया बाद में फिर प्रयास करें।", "नेटवर्क कनेक्शन त्रुटि, कृपया अपना नेटवर्क जाँचें।", "VPN या प्रॉक्सी बंद करके फिर प्रयास करें।", "यह सेवा अभी आपके क्षेत्र में उपलब्ध नहीं है।", "वर्तमान ऐप वातावरण में समस्या मिली है।", "कृपया बाद में फिर प्रयास करें।", "पुष्टि करें") },
            { "is", new LanguagePack("Netvilla, reyndu aftur síðar.", "Villa í nettengingu, athugaðu netið þitt.", "Slökktu á VPN eða proxy og reyndu aftur.", "Þessi þjónusta er ekki í boði á þínu svæði eins og er.", "Vandamál fannst í núverandi umhverfi forritsins.", "Reyndu aftur síðar.", "Staðfesta") },
            { "it", new LanguagePack("Errore di rete, riprova più tardi.", "Errore di connessione di rete, controlla la rete.", "Disattiva la VPN o il proxy e riprova.", "Questo servizio non è attualmente disponibile nella tua area.", "È stato rilevato un problema nell’ambiente attuale dell’app.", "Riprova più tardi.", "Conferma") },
            { "sw", new LanguagePack("Hitilafu ya mtandao, tafadhali jaribu tena baadaye.", "Hitilafu ya muunganisho wa mtandao, tafadhali angalia mtandao wako.", "Zima VPN au proksi kisha ujaribu tena.", "Huduma hii haipatikani kwa sasa katika eneo lako.", "Tatizo limegunduliwa katika mazingira ya sasa ya programu.", "Tafadhali jaribu tena baadaye.", "Thibitisha") },
            { "ky", new LanguagePack("Тармак катасы, кийинчерээк кайра аракет кылыңыз.", "Тармакка туташуу катасы, тармагыңызды текшериңиз.", "VPN же проксини өчүрүп, кайра аракет кылыңыз.", "Бул кызмат учурда сиздин аймакта жеткиликсиз.", "Колдонмонун учурдагы чөйрөсүндө көйгөй табылды.", "Кийинчерээк кайра аракет кылыңыз.", "Ырастоо") },
            { "km", new LanguagePack("បញ្ហាបណ្ដាញ សូមព្យាយាមម្ដងទៀតនៅពេលក្រោយ។", "បញ្ហាការតភ្ជាប់បណ្ដាញ សូមពិនិត្យបណ្ដាញរបស់អ្នក។", "សូមបិទ VPN ឬប្រូកស៊ី ហើយព្យាយាមម្ដងទៀត។", "សេវាកម្មនេះមិនអាចប្រើបាននៅតំបន់របស់អ្នកនៅពេលនេះទេ។", "បានរកឃើញបញ្ហានៅក្នុងបរិស្ថានកម្មវិធីបច្ចុប្បន្ន។", "សូមព្យាយាមម្ដងទៀតនៅពេលក្រោយ។", "បញ្ជាក់") },
            { "lo", new LanguagePack("ເຄືອຂ່າຍຂັດຂ້ອງ, ກະລຸນາລອງໃໝ່ພາຍຫຼັງ.", "ການເຊື່ອມຕໍ່ເຄືອຂ່າຍຂັດຂ້ອງ, ກະລຸນາກວດສອບເຄືອຂ່າຍ.", "ປິດ VPN ຫຼື proxy ແລ້ວລອງໃໝ່.", "ບໍລິການນີ້ຍັງບໍ່ຮອງຮັບໃນພື້ນທີ່ຂອງທ່ານ.", "ພົບບັນຫາໃນສະພາບແວດລ້ອມແອັບປັດຈຸບັນ.", "ກະລຸນາລອງໃໝ່ພາຍຫຼັງ.", "ຢືນຢັນ") },
            { "si", new LanguagePack("ජාල දෝෂයකි, කරුණාකර පසුව නැවත උත්සාහ කරන්න.", "ජාල සම්බන්ධතා දෝෂයකි, ඔබගේ ජාලය පරීක්ෂා කරන්න.", "VPN හෝ ප්‍රොක්සි අක්‍රිය කර නැවත උත්සාහ කරන්න.", "මෙම සේවාව දැනට ඔබගේ ප්‍රදේශයේ නොමැත.", "වත්මන් යෙදුම් පරිසරයේ ගැටලුවක් හඳුනාගෙන ඇත.", "කරුණාකර පසුව නැවත උත්සාහ කරන්න.", "තහවුරු කරන්න") },
            { "lt", new LanguagePack("Tinklo klaida, bandykite dar kartą vėliau.", "Tinklo ryšio klaida, patikrinkite savo tinklą.", "Išjunkite VPN arba tarpinį serverį ir bandykite dar kartą.", "Ši paslauga šiuo metu jūsų regione nepasiekiama.", "Dabartinėje programos aplinkoje aptikta problema.", "Bandykite dar kartą vėliau.", "Patvirtinti") },
            { "lb", new LanguagePack("Netzwierkfeeler, probéiert méi spéit nach eng Kéier.", "Feeler bei der Netzwierkverbindung, kontrolléiert Äert Netzwierk.", "Desaktivéiert VPN oder Proxy a probéiert nach eng Kéier.", "Dëse Service ass de Moment net an Ärer Regioun verfügbar.", "E Problem gouf an der aktueller App-Ëmfeld erkannt.", "Probéiert méi spéit nach eng Kéier.", "Bestätegen") },
            { "lv", new LanguagePack("Tīkla kļūda, lūdzu, mēģiniet vēlreiz vēlāk.", "Tīkla savienojuma kļūda, pārbaudiet savu tīklu.", "Izslēdziet VPN vai starpniekserveri un mēģiniet vēlreiz.", "Šis pakalpojums pašlaik nav pieejams jūsu reģionā.", "Pašreizējā lietotnes vidē konstatēta problēma.", "Lūdzu, mēģiniet vēlreiz vēlāk.", "Apstiprināt") },
            { "ro", new LanguagePack("Eroare de rețea, încercați din nou mai târziu.", "Eroare de conexiune la rețea, verificați rețeaua.", "Dezactivați VPN-ul sau proxy-ul și încercați din nou.", "Acest serviciu nu este disponibil momentan în regiunea dvs.", "A fost detectată o problemă în mediul actual al aplicației.", "Încercați din nou mai târziu.", "Confirmă") },
            { "sr", new LanguagePack("Мрежна грешка, покушајте поново касније.", "Грешка мрежне везе, проверите мрежу.", "Искључите VPN или прокси и покушајте поново.", "Ова услуга тренутно није доступна у вашем региону.", "Откривен је проблем у тренутном окружењу апликације.", "Покушајте поново касније.", "Потврди") },
            { "mg", new LanguagePack("Nisy hadisoana ny tambajotra, andramo indray afaka kelikely.", "Nisy hadisoana ny fifandraisana, jereo ny tambajotrao.", "Vonoy ny VPN na proxy dia andramo indray.", "Tsy misy amin’izao fotoana izao ity tolotra ity ao amin’ny faritra misy anao.", "Nahitana olana ny tontolon’ny fampiharana ankehitriny.", "Andramo indray afaka kelikely.", "Hamarino") },
            { "mk", new LanguagePack("Мрежна грешка, обидете се повторно подоцна.", "Грешка во мрежната врска, проверете ја мрежата.", "Исклучете VPN или прокси и обидете се повторно.", "Оваа услуга моментално не е достапна во вашиот регион.", "Откриен е проблем во тековната околина на апликацијата.", "Обидете се повторно подоцна.", "Потврди") },
            { "mn", new LanguagePack("Сүлжээний алдаа гарлаа. Дараа дахин оролдоно уу.", "Сүлжээний холболтын алдаа. Сүлжээгээ шалгана уу.", "VPN эсвэл проксиг унтраагаад дахин оролдоно уу.", "Энэ үйлчилгээ одоогоор таны бүсэд боломжгүй байна.", "Аппын одоогийн орчинд асуудал илэрлээ.", "Дараа дахин оролдоно уу.", "Батлах") },
            { "mt", new LanguagePack("Żball fin-netwerk, erġa’ pprova aktar tard.", "Żball fil-konnessjoni tan-netwerk, iċċekkja n-netwerk tiegħek.", "Itfi l-VPN jew il-proxy u erġa’ pprova.", "Dan is-servizz bħalissa mhux disponibbli fir-reġjun tiegħek.", "Instabet problema fl-ambjent attwali tal-app.", "Erġa’ pprova aktar tard.", "Ikkonferma") },
            { "ny", new LanguagePack("Vuto la netiweki, yesaninso nthawi ina.", "Kulumikizana kwa netiweki kwalephera, onani netiweki yanu.", "Zimitsani VPN kapena proxy ndipo yesaninso.", "Ntchitoyi sikupezeka m'dera lanu pakali pano.", "Vuto lapezeka pa momwe pulogalamuyi ikuyendera.", "Yesaninso nthawi ina.", "Tsimikizani") },
            { "ne", new LanguagePack("नेटवर्क त्रुटि भयो, कृपया पछि फेरि प्रयास गर्नुहोस्।", "नेटवर्क जडान त्रुटि भयो, आफ्नो नेटवर्क जाँच गर्नुहोस्।", "VPN वा प्रोक्सी बन्द गरेर फेरि प्रयास गर्नुहोस्।", "यो सेवा हाल तपाईंको क्षेत्रमा उपलब्ध छैन।", "हालको एप वातावरणमा समस्या भेटिएको छ।", "कृपया पछि फेरि प्रयास गर्नुहोस्।", "पुष्टि गर्नुहोस्") },
            { "nb", new LanguagePack("Nettverksfeil, prøv igjen senere.", "Feil med nettverkstilkoblingen, kontroller nettverket ditt.", "Deaktiver VPN eller proxy og prøv igjen.", "Denne tjenesten er for øyeblikket ikke tilgjengelig i området ditt.", "Det ble oppdaget et problem i det gjeldende appmiljøet.", "Prøv igjen senere.", "Bekreft") },
            { "fil", new LanguagePack("Error sa network, pakisubukang muli mamaya.", "Error sa koneksyon ng network, pakisuri ang iyong network.", "I-off ang VPN o proxy at subukang muli.", "Kasalukuyang hindi available ang serbisyong ito sa iyong rehiyon.", "May nakitang problema sa kasalukuyang kapaligiran ng app.", "Pakisubukang muli mamaya.", "Kumpirmahin") },
            { "pl", new LanguagePack("Błąd sieci, spróbuj ponownie później.", "Błąd połączenia sieciowego, sprawdź swoją sieć.", "Wyłącz VPN lub serwer proxy i spróbuj ponownie.", "Ta usługa jest obecnie niedostępna w Twoim regionie.", "Wykryto problem w bieżącym środowisku aplikacji.", "Spróbuj ponownie później.", "Potwierdź") },
            { "rw", new LanguagePack("Ikosa ry’umuyoboro, ongera ugerageze nyuma.", "Ikosa ryo guhuza umuyoboro, genzura umuyoboro wawe.", "Funga VPN cyangwa porogisi maze wongere ugerageze.", "Iyi serivisi ntiboneka ubu mu karere kawe.", "Habonetse ikibazo mu mikorere ya porogaramu.", "Ongera ugerageze nyuma.", "Emeza") },
            { "sv", new LanguagePack("Nätverksfel, försök igen senare.", "Fel på nätverksanslutningen, kontrollera ditt nätverk.", "Inaktivera VPN eller proxy och försök igen.", "Den här tjänsten är för närvarande inte tillgänglig i din region.", "Ett problem upptäcktes i den aktuella appmiljön.", "Försök igen senare.", "Bekräfta") },
            { "sl", new LanguagePack("Napaka omrežja, poskusite znova pozneje.", "Napaka omrežne povezave, preverite omrežje.", "Izklopite VPN ali proxy in poskusite znova.", "Ta storitev trenutno ni na voljo v vaši regiji.", "V trenutnem okolju aplikacije je bila zaznana težava.", "Poskusite znova pozneje.", "Potrdi") },
            { "sk", new LanguagePack("Chyba siete, skúste to znova neskôr.", "Chyba sieťového pripojenia, skontrolujte svoju sieť.", "Vypnite VPN alebo proxy a skúste to znova.", "Táto služba momentálne nie je vo vašom regióne dostupná.", "V aktuálnom prostredí aplikácie sa zistil problém.", "Skúste to znova neskôr.", "Potvrdiť") },
            { "af", new LanguagePack("Netwerkfout, probeer asseblief later weer.", "Netwerkverbindingsfout, kontroleer asseblief jou netwerk.", "Skakel jou VPN of instaanbediener af en probeer weer.", "Hierdie diens is tans nie in jou streek beskikbaar nie.", "’n Probleem is in die huidige toepassingomgewing bespeur.", "Probeer asseblief later weer.", "Bevestig") },
            { "th", new LanguagePack("เกิดข้อผิดพลาดของเครือข่าย โปรดลองอีกครั้งภายหลัง", "การเชื่อมต่อเครือข่ายผิดพลาด โปรดตรวจสอบเครือข่ายของคุณ", "โปรดปิด VPN หรือพร็อกซีแล้วลองอีกครั้ง", "ขณะนี้บริการนี้ไม่พร้อมใช้งานในภูมิภาคของคุณ", "ตรวจพบปัญหาในสภาพแวดล้อมปัจจุบันของแอป", "โปรดลองอีกครั้งภายหลัง", "ยืนยัน") },
            { "tg", new LanguagePack("Хатои шабака, баъдтар аз нав кӯшиш кунед.", "Хатои пайвасти шабака, шабакаи худро санҷед.", "VPN ё проксиро хомӯш карда, аз нав кӯшиш кунед.", "Ин хидмат ҳоло дар минтақаи шумо дастрас нест.", "Дар муҳити ҷории барнома мушкил ошкор шуд.", "Баъдтар аз нав кӯшиш кунед.", "Тасдиқ") },
            { "uk", new LanguagePack("Помилка мережі. Спробуйте ще раз пізніше.", "Помилка підключення до мережі. Перевірте мережу.", "Вимкніть VPN або проксі та спробуйте ще раз.", "Ця послуга наразі недоступна у вашому регіоні.", "Виявлено проблему в поточному середовищі застосунку.", "Спробуйте ще раз пізніше.", "Підтвердити") },
            { "uz", new LanguagePack("Tarmoq xatosi, keyinroq qayta urinib ko‘ring.", "Tarmoq ulanishida xato, tarmog‘ingizni tekshiring.", "VPN yoki proksini o‘chirib, qayta urinib ko‘ring.", "Bu xizmat hozircha hududingizda mavjud emas.", "Joriy ilova muhitida muammo aniqlandi.", "Keyinroq qayta urinib ko‘ring.", "Tasdiqlash") },
        };

    private static readonly Dictionary<string, string> BtnTexts = new Dictionary<string, string>()
        {
            { "zh-CN", "确定" },
            { "en-US", "Confirm" },
            { "pt-BR", "Confirmar" },
            { "id-ID", "Konfirmasi" },
            { "ja-JP", "確認" },
            { "ko-KR", "확인" },
            { "es-ES", "Confirmar" },
            { "es-MX", "Confirmar" },
            { "ar-SA", "تأكيد" },
            { "de-DE", "Bestätigen" },
            { "ru-RU", "Подтвердить" },
            { "tr-TR", "Onayla" },
            { "en-PH", "Confirm" },
            { "vi-VN", "Xác nhận" },
        };

    private static readonly Dictionary<string, string> TitleTexts =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "zh-CN", "通知" },
            { "en-US", "Notification" },
            { "pt-BR", "Notificação" },
            { "id-ID", "Notifikasi" },
            { "ja-JP", "お知らせ" },
            { "ko-KR", "알림" },
            { "es-ES", "Notificación" },
            { "es-MX", "Notificación" },
            { "ar-SA", "إشعار" },
            { "de-DE", "Benachrichtigung" },
            { "ru-RU", "Уведомление" },
            { "tr-TR", "Bildirim" },
            { "en-PH", "Notification" },
            { "vi-VN", "Thông báo" },
            { "sq", "Njoftim" },
            { "hy", "Ծանուցում" },
            { "az", "Bildiriş" },
            { "bs", "Obavijest" },
            { "bn", "বিজ্ঞপ্তি" },
            { "nl", "Melding" },
            { "fr", "Notification" },
            { "bg", "Известие" },
            { "rn", "Itangazo" },
            { "ms", "Pemberitahuan" },
            { "el", "Ειδοποίηση" },
            { "cs", "Upozornění" },
            { "da", "Meddelelse" },
            { "et", "Teavitus" },
            { "am", "ማሳወቂያ" },
            { "fi", "Ilmoitus" },
            { "ka", "შეტყობინება" },
            { "hr", "Obavijest" },
            { "ht", "Notifikasyon" },
            { "hu", "Értesítés" },
            { "he", "התראה" },
            { "hi", "सूचना" },
            { "is", "Tilkynning" },
            { "it", "Notifica" },
            { "sw", "Arifa" },
            { "ky", "Билдирүү" },
            { "km", "ការជូនដំណឹង" },
            { "lo", "ແຈ້ງການ" },
            { "si", "දැනුම්දීම" },
            { "lt", "Pranešimas" },
            { "lb", "Notifikatioun" },
            { "lv", "Paziņojums" },
            { "ro", "Notificare" },
            { "sr", "Обавештење" },
            { "mg", "Fampahafantarana" },
            { "mk", "Известување" },
            { "mn", "Мэдэгдэл" },
            { "mt", "Notifika" },
            { "ny", "Chidziwitso" },
            { "ne", "सूचना" },
            { "nb", "Varsel" },
            { "fil", "Abiso" },
            { "pl", "Powiadomienie" },
            { "rw", "Imenyesha" },
            { "sv", "Meddelande" },
            { "sl", "Obvestilo" },
            { "sk", "Upozornenie" },
            { "af", "Kennisgewing" },
            { "th", "การแจ้งเตือน" },
            { "tg", "Огоҳинома" },
            { "uk", "Сповіщення" },
            { "uz", "Bildirishnoma" },
        };

    public struct Args
    {
        public bool isLanguage;
        public string des;
        public Action onFinish;
    }

    public Text desTxt;
    public Button confirmBtn;
    public Text btnTxt;

    public Text titleTxt;

    private Args _args;

    private void OnEnable()
    {
        confirmBtn.onClick.AddListener(CloseSelf);
    }

    public void OnOpen(CommonConfirmTipsPanel.Args args)
    {
        string selectedLanguage = PlayerPrefs.GetString(SelectedLanguageKey, DefaultLanguage);
        desTxt.text = args.isLanguage ? GetLanguageText(args.des, selectedLanguage) : args.des;
        Debug.Log($"[CommonConfirmTipsPanel] 提示内容：{args.des}");
        _args = args;
        btnTxt.text = GetButtonText(selectedLanguage);
        titleTxt.text = GetTitleText(selectedLanguage);
    }

    public static string GetTokenClientMessageKey(string publicReason)
    {
        switch ((publicReason ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "VPN":
                return "TokenClient_VPN";
            case "REGION":
                return "TokenClient_REGION";
            case "APP_INTEGRITY":
                return "TokenClient_APP_INTEGRITY";
            case "TOKEN_INVALID":
                return "TokenClient_TOKEN_INVALID";
            default:
                return "TokenClient_Default";
        }
    }

    private static string GetLanguageText(string key, string selectedLanguage)
    {
        if (string.IsNullOrEmpty(key) || !LanguageMap.TryGetValue(key, out var languageTexts))
        {
            return key;
        }

        
        if (languageTexts.TryGetValue(selectedLanguage, out var text))
        {
            return text;
        }

        string languagePrefix = GetLanguagePrefix(selectedLanguage);
        foreach (var languageText in languageTexts)
        {
            if (string.Equals(GetLanguagePrefix(languageText.Key), languagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return languageText.Value;
            }
        }

        if (AdditionalLanguagePacks.TryGetValue(languagePrefix, out var languagePack))
        {
            string additionalText = languagePack.GetText(key);
            if (!string.IsNullOrEmpty(additionalText))
            {
                return additionalText;
            }
        }

        return languageTexts.TryGetValue(DefaultLanguage, out var defaultText)
            ? defaultText
            : key;
    }

    private static string GetLanguagePrefix(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return string.Empty;
        }

        int separatorIndex = languageCode.IndexOf('-');
        return separatorIndex > 0
            ? languageCode.Substring(0, separatorIndex)
            : languageCode;
    }

    private static string GetTitleText(string selectedLanguage)
    {
        if (!string.IsNullOrEmpty(selectedLanguage) &&
            TitleTexts.TryGetValue(selectedLanguage, out string titleText))
        {
            return titleText;
        }

        string languagePrefix = GetLanguagePrefix(selectedLanguage);
        foreach (var item in TitleTexts)
        {
            if (string.Equals(
                    GetLanguagePrefix(item.Key),
                    languagePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        return TitleTexts[DefaultLanguage];
    }

    private static string GetButtonText(string selectedLanguage)
    {
        if (!string.IsNullOrEmpty(selectedLanguage) &&
            BtnTexts.TryGetValue(selectedLanguage, out string buttonText))
        {
            return buttonText;
        }

        string languagePrefix = GetLanguagePrefix(selectedLanguage);
        foreach (var item in BtnTexts)
        {
            if (string.Equals(
                    GetLanguagePrefix(item.Key),
                    languagePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        if (AdditionalLanguagePacks.TryGetValue(languagePrefix, out var languagePack))
        {
            return languagePack.Confirm;
        }

        return BtnTexts[DefaultLanguage];
    }

    private void OnDisable()
    {
        _args.onFinish?.Invoke();
        confirmBtn.onClick.RemoveListener(CloseSelf);
    }

    private void CloseSelf()
    {
        gameObject.SetActive(false);
    }
}
