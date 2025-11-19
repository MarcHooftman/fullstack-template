import { get, post, put, del } from "@/lib/api";

const useApi = () => {
    return {
        get,
        post,
        put,
        del
    };
};

export default useApi;