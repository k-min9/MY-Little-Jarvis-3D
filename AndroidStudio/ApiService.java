package com.example.mylittlejarvisandroid;

import com.google.gson.JsonObject;

import okhttp3.MultipartBody;
import okhttp3.RequestBody;
import retrofit2.Call;
import retrofit2.http.Multipart;
import retrofit2.http.POST;
import retrofit2.http.Part;

public interface ApiService {
    @Multipart
    @POST("stt") // 엔드포인트
    Call<JsonObject> uploadAudio(
            @Part MultipartBody.Part file,
            @Part("lang") RequestBody lang,
            @Part("level") RequestBody level,
            @Part("chatIdx") RequestBody chatIdx
    );
}
